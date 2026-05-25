using System;
using System.Runtime.InteropServices;
using System.Threading;
using GcLib.Utilities.Imaging;
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;
using Microsoft.Extensions.Logging;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to the IDSImaging Peak API from IDS Imaging Development Systems.
/// </summary>
public sealed partial class IdsCam
{
    /// <summary>
    /// GenTL datastream used for image acquisition.
    /// </summary>
    private DataStream _dataStream;

    /// <summary>
    /// Image acquisition thread.
    /// </summary>
    private Thread _imageAcquisitionThread;

    /// <summary>
    /// Image acquisition thread stopping signal.
    /// </summary>
    private bool _threadIsRunning;

    /// <summary>
    /// PC time when acquisition is started (given in PC ticks, where a single tick represents one hundred nanoseconds or one ten-millionth of a second).
    /// </summary>
    private ulong _pcTime0;

    /// <inheritdoc/>
    public override uint PayloadSize => (uint)_nodeMap.FindNode<IntegerNode>("PayloadSize").Value();

    /// <inheritdoc/>
    public override uint BufferCapacity { get; set; }

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        // Retrieve opened datastream.
        _dataStream = _device.DataStreams()[0].OpenedDataStream();

        // Minimum number of required buffers.
        var minBufferCountRequired = _dataStream.NumBuffersAnnouncedMinRequired();

        // Allocate buffers and add them to the pool
        for (var i = 0; i < minBufferCountRequired; ++i)
        {
            var buffer = _dataStream.AllocAndAnnounceBuffer(PayloadSize, IntPtr.Zero);
            _dataStream.QueueBuffer(buffer);
        }

        // PC time at acquisition start.
        _pcTime0 = (ulong)DateTime.Now.Ticks;

        // Reset camera internal timings at acquisition start. (assumes that TimestampReset is available and supported by the device, is this safe?)
        _nodeMap.FindNodeCommand("TimestampReset").Execute();
        _nodeMap.FindNodeCommand("TimestampReset").WaitUntilDone();

        // Start acquisition on datastream and device.
        _dataStream.StartAcquisition();
        _nodeMap.FindNode<CommandNode>("AcquisitionStart").Execute();
        _nodeMap.FindNode<CommandNode>("AcquisitionStart").WaitUntilDone();

        // Start image acquisition thread.
        _imageAcquisitionThread = new Thread(ImageAcquisitionThread) { Name = "ImageAcquisitionThread (IdsCam)" };
        _threadIsRunning = true;
        _imageAcquisitionThread.Start();

        IsAcquiring = true;

        // Announce event.
        OnAcquisitionStarted();
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        if (IsAcquiring == false)
            return;

        // Stop image acquisition thread.
        _threadIsRunning = false;
        _imageAcquisitionThread.Join();

        // Stop acquisition on datastream and device.
        _dataStream.StopAcquisition(AcquisitionStopMode.Default);

        // Flush datastream to clear out any buffers that may still be in the stream.
        _dataStream.Flush(DataStreamFlushMode.DiscardAll);

        // Revoke all buffers in the pool and free their resources.
        foreach (var buffer in _dataStream.AnnouncedBuffers())
        {
            // Remove buffer from the transport layer
            _dataStream.RevokeBuffer(buffer);
        }

        IsAcquiring = false;

        // Close datastream.
        _device.DataStreams()[0].OpenedDataStream().Dispose();

        // Announce event.
        OnAcquisitionStopped();
    }

    /// <summary>
    /// Image acquisition thread. Attempts to retrieve buffers from datastream and signals a new buffer event if successfull.
    /// </summary>
    private void ImageAcquisitionThread()
    {
        // Log debugging info.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", DeviceInfo.ModelName, DeviceInfo.UniqueID);

        while (_threadIsRunning)
        {
            try
            {
                // Retrieve buffer from datastream.
                var buffer = _dataStream.WaitForFinishedBuffer(3000);
                if (buffer.HasNewData())
                {
                    // Raise new buffer event.
                    OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(buffer), DateTime.Now));

                    // Release buffer to the datastream for reuse.
                    _dataStream.QueueBuffer(buffer);
                }
                else throw new Exception();
            }
            catch (Exception ex)
            {
                // Log error.
                if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                    GcLibrary.Logger.LogWarning(ex, "Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", DeviceInfo.ModelName, DeviceInfo.UniqueID);

                // Increment consecutive error count.
                OnFailedBuffer();
            }
        }

        // Log debugging info.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
            GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) stopped", DeviceInfo.ModelName, DeviceInfo.UniqueID);
    }

    /// <summary>
    /// Converts a buffer from the IDSImaging Peak API to a GcBuffer containing the image data and metadata.
    /// </summary>
    /// <param name="buffer">The buffer to convert.</param>
    /// <returns>The converted <see cref="GcBuffer"/>.</returns>
    private GcBuffer ToGcBuffer(IDSImaging.Peak.API.Core.Buffer buffer)
    {
        // Extract image data from buffer.
        byte[] imageData = new byte[buffer.Size()];
        Marshal.Copy(buffer.BasePtr(), imageData, 0, (int)buffer.Size());

        return new GcBuffer(imageData: imageData,
                            width: buffer.Width(),
                            height: buffer.Height(),
                            pixelFormat: (PixelFormat)buffer.PixelFormat(),
                            pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax((PixelFormat)buffer.PixelFormat()),
                            frameID: (long)buffer.FrameID(),
                            timeStamp: _pcTime0 + (ulong)Math.Round(buffer.Timestamp_ns() / 100d));
    }
}
