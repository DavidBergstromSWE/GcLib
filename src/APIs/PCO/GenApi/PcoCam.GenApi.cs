using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Microsoft.Extensions.Logging;
using PCO.SDK.NET;

namespace GcLib;

public partial class PcoCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Constants

        // Image parameter bits

        /// <summary>
        /// Next image transfers will be done from a recording camera.
        /// </summary>
        private const int IMAGEPARAMETERS_READ_WHILE_RECORDING = 0x0000001;

        // Buffer status

        /// <summary>
        /// Buffer event is set.
        /// </summary>
        private const uint BUFFER_EVENT_SET = 0xC0008000;

        // Error codes

        /// <summary>
        /// No errors (success).
        /// </summary>
        private const uint PCO_NOERROR = 0x00000000;

        #endregion

        #region Fields

        /// <summary>
        /// Camera (outer class) reference.
        /// </summary>
        private readonly PcoCam _pcoCam;

        /// <summary>
        /// Camera handle.
        /// </summary>
        private nint _cameraHandle;

        // Input buffer pool related
        // ToDo: Simplify interface to input buffer pool?

        /// <summary>
        /// Indices of buffers in input buffer pool.
        /// </summary>
        private short[] _bufferIndex;

        /// <summary>
        /// Handles to buffers in input buffer pool.
        /// </summary>
        private nuint[] _buffer;

        /// <summary>
        /// Event handles to buffers in input buffer pool.
        /// </summary>
        private nint[] _bufferEvent;

        // thread related

        /// <summary>
        /// Image acquisition thread.
        /// </summary>
        private Thread _imageAcquisitionThread;

        /// <summary>
        /// Image acquisition thread priority.
        /// </summary>
        private readonly ThreadPriority _threadPriority = ThreadPriority.Highest;

        /// <summary>
        /// Image acquisition thread stopping condition.
        /// </summary>
        private bool _threadIsRunning;

        /// <summary>
        /// Indicates if camera is armed or not.
        /// </summary>
        private bool _isArmed;

        #endregion

        #region Properties

        /// <summary>
        /// Device top-level information, containing name of vendor, model, serialnumber, unique id, device class and whether device is accessible or opened.
        /// </summary>
        public GcDeviceInfo DeviceInfo { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new GenApi module attached to camera.
        /// </summary>
        /// <param name="pcoCam">Camera.</param>
        /// <param name="deviceID">Unique ID (serial number) of device or null or empty string for next available one.</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="DivideByZeroException"></exception>
        /// <exception cref="PcoException"></exception>
        public GenApi(PcoCam pcoCam, string deviceID)
        {
            _pcoCam = pcoCam;

            // Reset camera handle.
            _cameraHandle = nint.Zero;

            // Find device.
            if (string.IsNullOrEmpty(deviceID))
            {
                // Get first available one.
                if (LibWrapper.OpenCamera(ref _cameraHandle) == false)
                    throw new ArgumentException("No device found!");
            }
            else
            {
                // Get specific one (using string identifier) by looping through all available.
                while (LibWrapper.OpenCamera(ref _cameraHandle))
                {

                    // Compare device ID with device serial number (used as ID).
                    if (LibWrapper.GetCameraSerialNumber(_cameraHandle) == deviceID)
                    {
                        // Found the camera!
                        break;
                    }
                    else
                    {
                        // Try next one...
                        _cameraHandle = nint.Zero;
                    }
                }
                if (_cameraHandle == nint.Zero)
                    throw new ArgumentException("No device found!");
            }

            // Fill device info.
            DeviceInfo = GetDeviceInfo(_cameraHandle);
            DeviceInfo.IsAccessible = false;
            DeviceInfo.IsOpen = true;

            // Initialize some camera settings that current code implementation requires.
            LibWrapper.ResetSettingsToDefault(_cameraHandle); // reset all camera settings to default values
            LibWrapper.SetTimestampMode(_cameraHandle, TimestampMode.Binary); // BCD coded timestamp and frame counter (first 14 pixels)
            LibWrapper.SetTriggerMode(_cameraHandle, PCO.SDK.NET.TriggerMode.AutoSequence); // auto-sequence trigger mode (signals at the trigger input line are irrelevant) - for external triggering this mode has to be changed! (see manual)

            // Initialize GenApi parameters/features.
            InitializeParameters();

            // Prepare input buffer pool.
            SetBufferCapacity((uint)InputBufferCount);

            // Subscribe to device events.
            _pcoCam.ParameterInvalidate += OnParameterChanged;
        }

        #endregion

        #region Public methods

        /// <inheritdoc/>
        public void Dispose()
        {
            try
            {
                // Make sure the camera is not recording.
                if (LibWrapper.GetRecordingState(_cameraHandle))
                    LibWrapper.SetRecordingState(_cameraHandle, false);

                // Free allocated buffers.
                for (int i = 0; i < _bufferIndex.Length; i++)
                    if (_bufferIndex[i] != -1)
                        LibWrapper.FreeBuffer(_cameraHandle, _bufferIndex[i]);

                // Close connection to camera.
                LibWrapper.CloseCamera(_cameraHandle);
            }
            catch (PcoException) { }
            finally
            {
                // Reset camera handle.
                _cameraHandle = nint.Zero;

                // Unsubscribe from device events.
                _pcoCam.ParameterInvalidate -= OnParameterChanged;
            }
        }

        /// <summary>
        /// Prepares required number of buffer contexts.
        /// </summary>
        /// <param name="bufCapacity">Number of buffers needed (maximum is 16).</param>
        /// <exception cref="ArgumentException"></exception>
        public void SetBufferCapacity(uint bufCapacity)
        {
            if (bufCapacity > 16)
                throw new ArgumentException("In using PCO SDK, a maximum of 16 buffers can be allocated per camera.");

            // Initialize array of PCO buffer contexts.
            _bufferEvent = new nint[bufCapacity];
            _bufferIndex = new short[bufCapacity];
            _buffer = new nuint[bufCapacity];
            for (int i = 0; i < _bufferIndex.Length; i++)
            {
                _bufferEvent[i] = nint.Zero;
                _bufferIndex[i] = -1; // makes sure to create new buffer
                _buffer[i] = nuint.Zero;
            }
        }

        /// <summary>
        /// Returns the payload size needed for the currently armed image size of camera.
        /// </summary>
        /// <returns>Payload size in bytes.</returns>
        /// <exception cref="PcoException"></exception>
        public uint GetPayloadSize()
        {
            ushort wXResAct = 0, wYResAct = 0, wXResMax = 0, wYResMax = 0;
            LibWrapper.GetSizes(_cameraHandle, ref wXResAct, ref wYResAct, ref wXResMax, ref wYResMax);
            return (uint)(wXResAct * wYResAct * LibWrapper.GetCameraBitDepth(_cameraHandle) / 8);
        }

        /// <summary>
        /// Retrieves lists of parameters implemented by camera.
        /// </summary>
        /// <param name="parameterList">List of successfully imported parameters and features implemented by camera.</param>
        /// <param name="failedParameterList">List of unsuccessfully imported parameters and features implemented by camera.</param>
        public void GetParameterList(out List<GcParameter> parameterList, out List<string> failedParameterList)
        {
            parameterList = [];
            failedParameterList = [];

            // Get properties (using Reflection).
            PropertyInfo[] propertyInfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            // Add GcParameter type properties to parameterList.
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.GetValue(this) is GcParameter gcParameter && gcParameter.IsImplemented)
                    parameterList.Add(gcParameter);
            }
        }

        /// <summary>
        /// Check if device connection is still available.
        /// </summary>
        /// <returns>true if connection is still valid, false otherwise.</returns>
        public bool IsAvailable()
        {
            // Check camera health.
            var isHealthy = LibWrapper.GetCameraHealth(_cameraHandle, out string warningMessage, out string errorMessage);

            if (isHealthy == false)
            {
                if (warningMessage != string.Empty)
                {
                    // Log warning.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Warning))
                        GcLibrary.Logger.LogWarning("Camera warning: {Warning}", warningMessage);

                    // display warning? throw exception?
                }

                if (errorMessage != string.Empty)
                {
                    // Log error.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                        GcLibrary.Logger.LogError("Camera error: {Error}", errorMessage);

                    // Close connection to camera.
                    LibWrapper.CloseCamera(_cameraHandle);
                    throw new InvalidOperationException("Connection to camera was closed, as a health status check failed. Please unplug camera to prevent damage!", new InvalidOperationException(errorMessage));
                }
            }

            return isHealthy;
        }

        #endregion
    }
}