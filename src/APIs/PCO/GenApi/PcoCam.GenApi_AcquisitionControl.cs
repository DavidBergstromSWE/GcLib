using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Logging;
using PCO.SDK.NET;
using PCO.SDK.NET.Utils;

namespace GcLib;

public partial class PcoCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Private fields

        private GcEnumeration _acquisitionMode;
        private GcInteger _acquisitionFrameCount;
        private GcFloat _acquisitionFrameRate;
        private GcFloat _exposureTime;
        private GcEnumeration _triggerMode;
        private GcInteger _inputBufferCount;

        /// <summary>
        /// Pre-allocated (managed) pool of byte arrays for storing acquired (unmanaged) buffer data.
        /// </summary>
        private byte[][] _inputBufferPool;

        #endregion

        // Provides properties for acquisition control.

        #region Acquisition Control

        /// <summary>
        /// Acquisition mode.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcEnumeration AcquisitionMode
        {
            get => _acquisitionMode;
            set => _acquisitionMode.IntValue = value;
        }

        /// <summary>
        /// Number of images to acquire for a <see cref="GcLib.AcquisitionMode.MultiFrame"/> acquisition mode.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcInteger AcquisitionFrameCount
        {
            get => _acquisitionFrameCount;
            set => _acquisitionFrameCount.Value = value;
        }

        /// <summary>
        /// Upper limit for acquisition rate at which frames are captured.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcFloat AcquisitionFrameRate
        {
            get => _acquisitionFrameRate;
            set => _acquisitionFrameRate.Value = value;
        }

        /// <summary>
        /// Exposure time.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcFloat ExposureTime
        {
            get => _exposureTime;
            set => _exposureTime.Value = value;
        }

        /// <summary>
        /// Arms (prepares) the camera for acquisition.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcCommand AcquisitionArm
        {
            get => new(
                name: "AcquisitionArm",
                category: "AcquisitionControl",
                description: "Arms the device before an AcquisitionStart command",
                method: () =>
                {
                    // Make sure the camera is not already recording.
                    if (LibWrapper.GetRecordingState(_cameraHandle))
                        LibWrapper.SetRecordingState(_cameraHandle, false);

                    ArmCamera();
                });

        }

        /// <summary>
        /// Provides a command to start an acquisition.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcCommand AcquisitionStart
        {
            get => new(
                name: "AcquisitionStart",
                category: "AcquisitionControl",
                description: "Starts the Acquisition of the device",
                method: () =>
                {
                    if (_pcoCam.IsAcquiring)
                        throw new InvalidOperationException($"Unable to start acquisition as Device {_pcoCam.DeviceInfo.ModelName} is already acquiring!");

                    if (_isArmed == false)
                        AcquisitionArm.Execute();

                    // Change recording state of camera to run (will wait until recording state is changed).
                    LibWrapper.SetRecordingState(_cameraHandle, true);

                    // Start new image acquisition thread.
                    _imageAcquisitionThread = new Thread(ImageAcquisitionThread)
                    {
                        Priority = _threadPriority,
                        Name = "Win32API"
                    };
                    _threadIsRunning = true;
                    _imageAcquisitionThread.Start();

                    _pcoCam.IsAcquiring = true;

                    _pcoCam.OnAcquisitionStarted();
                });
        }

        /// <summary>
        /// Provides a command to stop an acquisition.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcCommand AcquisitionStop
        {
            get => new(
                name: "AcquisitionStop",
                category: "AcquisitionControl",
                description: "Stops the Acquisition of the device",
                method: () =>
                {
                    if (_pcoCam.IsAcquiring == false)
                        return;

                    // Stop image acquisition thread.
                    _threadIsRunning = false;

                    // Wait for thread to terminate.
                    _imageAcquisitionThread.Join();

                    // Cancel all remaining buffers in input buffer pool.
                    LibWrapper.CancelImages(_cameraHandle);

                    // Set recording state to stop (will wait until recording state is changed).
                    LibWrapper.SetRecordingState(_cameraHandle, false);

                    _isArmed = false; // makes sure AcquisitionArm gets executed when next AcquisitionStart command is given

                    _pcoCam.IsAcquiring = false;

                    _pcoCam.OnAcquisitionStopped();
                });
        }

        /// <summary>
        /// Acquisition trigger mode.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcEnumeration TriggerMode
        {
            get => _triggerMode;
            set => _triggerMode.IntValue = value;
        }

        /// <summary>
        /// Number of buffers in the input buffer pool.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcInteger InputBufferCount
        {
            get => _inputBufferCount;
            set => _inputBufferCount.Value = value;
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Arms the camera to prepare it for recording, by validating current settings and allocating the buffers needed.
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private void ArmCamera()
        {
            // Reset timestamp to PC clock.
            //TimestampReset.Execute();

            // Ensure frame rate is updated.
            if (TriggerMode.IntValue == (int)PCO.SDK.NET.TriggerMode.ExternalExposureStart)
                OnParameterChanged(this, new ParameterInvalidateEventArgs(nameof(AcquisitionFrameRate)));

            // Arm camera.
            LibWrapper.ArmCamera(_cameraHandle);

            // Initialize managed input buffer pool.
            _inputBufferPool = new byte[InputBufferCount.Value][];

            // Calculate required payload size (GenTL GetInfo method?).
            uint payloadSize = GetPayloadSize(); // buffer size (in bytes)

            // Allocate buffers.
            for (int i = 0; i < _bufferIndex.Length; i++)
            {
                LibWrapper.AllocateBuffer(_cameraHandle, ref _bufferIndex[i], (int)payloadSize, ref _buffer[i], ref _bufferEvent[i]);
                _inputBufferPool[i] = new byte[payloadSize];
            }

            // Set image parameters for internal allocated resources (is this needed if PCO_ArmCamera is used?!).
            //_ = LibWrapper.PCO_SetImageParameters(_cameraHandle, width, height, IMAGEPARAMETERS_READ_WHILE_RECORDING);

            _isArmed = true;
        }

        /// <summary>
        /// Image acquisition thread method, based on methods in Win32 API (slightly modified version of example in AddBuffer_CamRun console application from PCO).
        /// </summary>
        private void ImageAcquisitionThread()
        {
            // Reset frame counter.
            var frameCounter = 0;

            // Get current bit depth.
            var bitDepth = LibWrapper.GetCameraBitDepth(_cameraHandle);

            // Set up image transfer requests.
            for (int i = 0; i < _bufferIndex.Length; i++)
                LibWrapper.AddBufferEx(cameraHandle: _cameraHandle, dwFirstImage: 0, dwLastImage: 0, sBufNr: _bufferIndex[i], wXRes: (ushort)Width, wYRes: (ushort)Height, wBitPerPixel: bitDepth);

            uint waitstat; // wait state (used by WaitForMultipleObjects and WaitForSingleObject)
            int test; // represents index of (initial) buffer to test for set event
            int multi; // counts the number of buffers which have their event set
            uint StatusDll = 0, StatusDrv = 0; // buffer status
            uint timeout = (uint)(10 / AcquisitionFrameRate.Value * 1000); // timeout in ms
            uint nBufferError = 0; // number of consecutive buffer errors
            uint bufferErrorLimit = 10; // number of consecutive buffer errors limit

            // Log debugging info.
            GcLibrary.Logger.LogTrace("Image acquisition thread (ID: {ThreadName}) in Device {ModelName} (ID: {ID}) started", _imageAcquisitionThread.Name, _pcoCam.DeviceInfo.ModelName, _pcoCam.DeviceInfo.UniqueID);

            // Main loop starts here.
            while (_threadIsRunning)
            {
                multi = 0;
                waitstat = WaitForMultipleObjects(nCount: (uint)_bufferIndex.Length, hHandle: _bufferEvent, bWaitAll: false, dwMilliseconds: timeout);

                // Time-out interval elapsed or failed (and the object's state is nonsignaled).
                if ((waitstat == WAIT_TIMEOUT) || (waitstat == WAIT_FAILED))
                {
                    if (waitstat == WAIT_FAILED)
                        GcLibrary.Logger.LogError("Error: acquisition failed in Device: {ModelName} (ID: {ID})", _pcoCam.DeviceInfo.ModelName, _pcoCam.DeviceInfo.UniqueID);
                    else GcLibrary.Logger.LogError("Error: acquisition timed out in Device: {ModelName} (ID: {ID})", _pcoCam.DeviceInfo.ModelName, _pcoCam.DeviceInfo.UniqueID);

                    // Increment consecutive error count.
                    _pcoCam.OnFailedBuffer();
                    nBufferError++;

                    // Abort thread if consecutive errors exceed limit.
                    if ((waitstat == WAIT_TIMEOUT) && (nBufferError > bufferErrorLimit))
                    {
                        _pcoCam.OnAcquisitionAborted(new AcquisitionAbortedEventArgs($"Acquisition timed out in Device: {_pcoCam.DeviceInfo.ModelName} (ID: {_pcoCam.DeviceInfo.UniqueID})!"));
                        break;
                    }
                    else if ((waitstat == WAIT_FAILED) && (nBufferError > bufferErrorLimit))
                    {
                        _pcoCam.OnAcquisitionAborted(new AcquisitionAbortedEventArgs($"Acquisition failed in Device: {_pcoCam.DeviceInfo.ModelName} (ID: {_pcoCam.DeviceInfo.UniqueID})!"));
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }

                // WaitForMultipleObjects might return with 2 or more events set, so all buffers must be checked.
                test = (int)(waitstat - WAIT_OBJECT_0); // gets the first (smallest) array index of the object that satisfied the wait
                for (int b = 0; b < _bufferIndex.Length; b++)
                {
                    waitstat = WaitForSingleObject(hHandle: _bufferEvent[test], dwMilliseconds: 0);
                    if (waitstat == WAIT_OBJECT_0) // The state of the specified object is signaled
                    {
                        multi++;
                        _ = ResetEvent(_bufferEvent[test]);
                        LibWrapper.GetBufferStatus(_cameraHandle, sBufNr: _bufferIndex[test], dwStatusDll: ref StatusDll, dwStatusDrv: ref StatusDrv);
                        //!!! IMPORTANT StatusDrv must always be checked for errors 
                        if (StatusDrv == 0) // no error in latest image transfer
                        {
                            if (multi > 1)
                            { /* multiple events found? */ }
                        }
                        else // error in transfer
                        {
                            // signal error here?
                            _pcoCam.OnFailedBuffer();
                            nBufferError++;
                            GcLibrary.Logger.LogWarning("Unsuccessful buffer transfer in Device: {modelName} (ID: {uniqueID})", _pcoCam.DeviceInfo.ModelName, _pcoCam.DeviceInfo.UniqueID);
                            break;
                        }

                        // Reset time outs.
                        nBufferError = 0;

                        // Raise new buffer event.
                        _pcoCam.OnNewBuffer(new NewBufferEventArgs(ToGcBuffer(_buffer[test], test, bitDepth), DateTime.Now));

                        frameCounter++;

                        // Check if acquisition is done.
                        if ((AcquisitionMode.StringValue == "SingleFrame") || (AcquisitionMode.StringValue == "MultiFrame" && frameCounter == AcquisitionFrameCount.Value))
                        {
                            // Acquisition is done, stop.
                            AcquisitionStop.Execute();
                            return;
                        }

                        // Set up new transfer request.
                        LibWrapper.AddBufferEx(_cameraHandle, dwFirstImage: 0, dwLastImage: 0, sBufNr: _bufferIndex[test], wXRes: (ushort)Width, wYRes: (ushort)Height, wBitPerPixel: bitDepth);
                    }

                    test++;
                    if (test >= _bufferIndex.Length)
                        test = 0; // restart index from 0
                }
            }

            // Log debugging info.
            GcLibrary.Logger.LogTrace("Image acquisition thread (ID: {ThreadName}) in Device {ModelName} (ID: {ID}) stopped", _imageAcquisitionThread.Name, _pcoCam.DeviceInfo.ModelName, _pcoCam.DeviceInfo.UniqueID);
        }

        /// <summary>
        /// Converts buffer pointer to <see cref="GcBuffer"/> image data container.
        /// </summary>
        /// <param name="bufferPtr">(Unmanaged) pointer to buffer data.</param>
        /// <returns>Image and chunkdata.</returns>
        private unsafe GcBuffer ToGcBuffer(nuint bufferPtr, int bufferIndex, uint bitDepth)
        {
            // Copy image data from unmanaged pointer to pre-allocated byte array in input buffer pool.
            Marshal.Copy((nint)bufferPtr.ToPointer(), _inputBufferPool[bufferIndex], 0, _inputBufferPool[bufferIndex].Length);

            // Extract image header (containing frame ID and timestamp) from first 14 pixels.
            var imageHeader = MemoryMarshal.Cast<byte, short>(new ReadOnlySpan<byte>(_inputBufferPool[bufferIndex], 0, 28));

            // Return new data container.
            return new GcBuffer(imageData: _inputBufferPool[bufferIndex],
                                width: (uint)Width,
                                height: (uint)Height,
                                pixelFormat: (PixelFormat)PixelFormat.IntValue,
                                pixelDynamicRangeMax: (uint)Math.Pow(2, bitDepth) - 1,
                                frameID: ImageHeaderHelper.GetImageNumber(imageHeader),
                                timeStamp: ImageHeaderHelper.GetTimestamp(imageHeader));
        }

        #endregion

        #region Win32

        /// <summary>
        /// The time-out interval elapsed, and the object's state is nonsignaled.
        /// </summary>
        private const long WAIT_TIMEOUT = 0x00000102L;

        /// <summary>
        /// The function has failed. To get extended error information, call GetLastError.
        /// </summary>
        private const long WAIT_FAILED = 0xFFFFFFFF;

        /// <summary>
        /// The state of the specified object is signaled.
        /// </summary>
        private const long WAIT_OBJECT_0 = 0x00000000L;

        /// <summary>
        /// Waits until the specified object is in the signaled state or the time-out interval elapses.
        /// </summary>
        /// <param name="hHandle">A handle to the object.</param>
        /// <param name="dwMilliseconds">The time-out interval, in milliseconds.</param>
        /// <returns>If the function succeeds, the return value indicates the event that caused the function to return.</returns>
        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial uint WaitForSingleObject(nint hHandle, uint dwMilliseconds);

        /// <summary>
        /// Waits until one or all of the specified objects are in the signaled state or the time-out interval elapses.
        /// </summary>
        /// <param name="nCount">The number of object handles in the array pointed to by hHandle.</param>
        /// <param name="hHandle">An array of object handles.</param>
        /// <param name="bWaitAll">If this parameter is TRUE, the function returns when the state of all objects in the hHandle array is signaled. 
        /// If FALSE, the function returns when the state of any one of the objects is set to signaled. 
        /// In the latter case, the return value indicates the object whose state caused the function to return.</param>
        /// <param name="dwMilliseconds">The time-out interval, in milliseconds. 
        /// If a nonzero value is specified, the function waits until the specified objects are signaled or the interval elapses.</param>
        /// <returns>If the function succeeds, the return value indicates the event that caused the function to return.</returns>
        [LibraryImport("kernel32.dll", SetLastError = true)]
        private static partial uint WaitForMultipleObjects(uint nCount, [In] nint[] hHandle, [MarshalAs(UnmanagedType.Bool)] bool bWaitAll, uint dwMilliseconds);

        /// <summary>
        /// Sets the specified event object to the nonsignaled state.
        /// </summary>
        /// <param name="hHandle">A handle to the event object.</param>
        /// <returns>If the function succeeds, the return value is true.</returns>
        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool ResetEvent(nint hHandle);

        #endregion
    }
}