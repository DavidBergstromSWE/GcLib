using System;
using IDSImaging.Peak.API.Core;
using IDSImaging.Peak.API.Core.Nodes;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to the IDSImaging Peak API from IDS Imaging Development Systems.
/// </summary>
public sealed partial class IdsCam
{
    private DataStream _dataStream;

    /// <inheritdoc/>
    public override uint PayloadSize => (uint)_nodeMap.FindNode<IntegerNode>("PayloadSize").Value();

    /// <inheritdoc/>
    public override uint BufferCapacity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        if (IsAcquiring)
            throw new InvalidOperationException($"Unable to start acquisition as Device {DeviceInfo.ModelName} is already acquiring!");

        _dataStream = _device.DataStreams()[0].OpenDataStream();

        // Minimum number of required buffers.
        var minBufferCountRequired = _dataStream.NumBuffersAnnouncedMinRequired();

        // Allocate buffers and add them to the pool
        for (var i = 0; i < minBufferCountRequired; ++i)
        {
            var buffer = _dataStream.AllocAndAnnounceBuffer(PayloadSize, IntPtr.Zero);
            _dataStream.QueueBuffer(buffer);
        }

        _dataStream.StartAcquisition();
        _nodeMap.FindNode<CommandNode>("AcquisitionStart").Execute();
        _nodeMap.FindNode<CommandNode>("AcquisitionStart").WaitUntilDone();

        IsAcquiring = true;

        // Announce event.
        OnAcquisitionStarted();
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        if (IsAcquiring == false)
            return;

        _dataStream.StopAcquisition(AcquisitionStopMode.Default);

        _dataStream.Flush(DataStreamFlushMode.DiscardAll);

        foreach (var buffer in _dataStream.AnnouncedBuffers())
        {
            // Remove buffer from the transport layer
            _dataStream.RevokeBuffer(buffer);
        }

        IsAcquiring = false;

        // Announce event.
        OnAcquisitionStopped();
    }
}
