using System;
using System.Collections.Generic;
using GcLib.Utilities.IO;
using PvDotNet;

namespace GcLib;

/// <summary>
/// Vendor-specific device class providing an interface to the eBUS SDK from Pleora Technologies.
/// </summary>
public sealed partial class PvCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    #region Fields

    // SDK-specific fields

    /// <summary>
    /// System-level class in eBUS SDK. Finds interfaces (network adapters and USB controlls) and devices (GigE Vision, USB3 Vision, Pleora Protocol) reachable from PC.
    /// </summary>
    private readonly PvSystem _pvSystem;

    /// <summary>
    /// Device-level class in eBUS SDK. Connects, configures and controls GigE Vision or USB3 Vision devices. 
    /// </summary>
    private readonly PvDevice _pvDevice;

    /// <summary>
    /// Class containing top-level information about device.
    /// </summary>
    private readonly PvDeviceInfo _pvDeviceInfo;

    /// <summary>
    /// Datastream-level class in eBUS SDK: Receives data from a GigE Vision or USB3 Vision transmitter. 
    /// </summary>
    private readonly PvStream _pvStream;

    /// <summary>
    /// Helper class managing the allocation of buffers.
    /// </summary>
    private readonly PvPipeline _pvPipeline;

    /// <summary>
    /// Helper class managing starting and stopping of acquisitions.
    /// </summary>
    private readonly PvAcquisitionStateManager _pvAcquisitionStateManager;

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="deviceID">(Optional) Unique string identifier for device.</param>
    public PvCam(string deviceID = "") : base()
    {
        // Find camera devices reachable from PC.
        _pvSystem = new PvSystem { DetectionTimeout = 500 }; // time to search the system for devices (ms)
        _pvSystem.Find();

        if (string.IsNullOrEmpty(deviceID))
        {
            // Get first available device.
            if (_pvSystem.DeviceCount > 0)
                _pvDeviceInfo = _pvSystem.GetDeviceInfo(0);
        }
        else
        {
            // Get specific device.
            _pvDeviceInfo = _pvSystem.FindDevice(deviceID);
        }

        if (_pvDeviceInfo == null)
            throw new ArgumentException("No camera found!");

        try
        {
            // Dynamically allocate PvDevice and PvStream objects of the right type.
            _pvDevice = PvDevice.CreateAndConnect(_pvDeviceInfo);
            _pvStream = PvStream.CreateAndOpen(_pvDeviceInfo);

            // Instantiate acquisition manager.
            _pvAcquisitionStateManager = new PvAcquisitionStateManager(_pvDevice, _pvStream);
            _pvAcquisitionStateManager.OnAcquisitionStateChanged += OnAcquisitionStateChanged;

            // Initialize properties.
            BufferCapacity = 4;

            // Configure acquisition pipeline.
            _pvPipeline = new PvPipeline(_pvStream)
            {
                BufferSize = _pvDevice.PayloadSize,
                BufferCount = BufferCapacity
            };

            // Start pipeline.
            _pvPipeline.Start();

            // GigEVision specific settings.
            if (_pvDeviceInfo.Type == PvDeviceInfoType.GEV)
            {
                var lDeviceGEV = _pvDevice as PvDeviceGEV;
                var lStreamGEV = _pvStream as PvStreamGEV;

                // Negotiate packet size.
                lDeviceGEV.NegotiatePacketSize();

                // Set stream destination to stream object.
                lDeviceGEV.SetStreamDestination(lStreamGEV.LocalIPAddress, lStreamGEV.LocalPort);

                // Uncomment to avoid timeout issues during debugging.
                //lDeviceGEV.CommunicationParameters.SetIntegerValue("HeartbeatInterval", 50000); 
            }

            // Update device info.
            DeviceInfo = GetDeviceInfo(_pvDeviceInfo);
            DeviceInfo.IsOpen = true;
            DeviceInfo.IsAccessible = false;

            // Retrieve collection of camera parameters.
            Parameters = ImportParameters();

            // Hook handler to OnLinkDisconnected events.
            _pvDevice.OnLinkDisconnected += OnLinkDisconnected;
        }
        catch (PvException)
        {
            throw;
        }

        // Special settings added here.

        //try
        //{
        //    // Reset triggering at connection (ett s.k. "fulhack").
        //    // ToDo: find better way to reset triggering settings.
        //    if (Parameters.IsImplemented("SyncMode"))
        //        Parameters.SetParameterValue("SyncMode", "Disabled");

        //    Parameters.Update();
        //}
        //catch (Exception)
        //{
        //    // Do nothing.
        //}
    }

    /// <summary>
    /// Handler to events raised when connection to device was lost.
    /// </summary>
    /// <param name="aDevice"></param>
    private void OnLinkDisconnected(PvDevice aDevice)
    {
        // Announce connection lost.
        OnConnectionLost();
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public override void Close()
    {
        base.Close();

        _pvAcquisitionStateManager?.OnAcquisitionStateChanged -= OnAcquisitionStateChanged;

        if (_pvPipeline != null)
        {
            if (_pvPipeline.IsStarted)
                _pvPipeline.Stop();
            _pvPipeline.Dispose();
        }

        // Close PvStream.
        if (_pvStream != null)
        {
            if (_pvStream.IsOpen)
                _pvStream.Close();
            _pvStream.Dispose();
        }

        // Unhook OnLinkDisconnected handler (to prevent it from raising).
        _pvDevice.OnLinkDisconnected -= OnLinkDisconnected;

        // Disconnect PvDevice.
        if (_pvDevice != null)
        {
            if (_pvDevice.IsConnected)
                _pvDevice.Disconnect();
            //_pvDevice.Dispose(); // hangs here on OnLinkDisconnected?
        }

        _pvDeviceInfo.Dispose();
        _pvSystem.Dispose();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Get top-level info about device.
    /// </summary>
    /// <param name="pvDeviceInfo">Device information used to connect the device.</param>
    /// <returns>Device info.</returns>
    private static GcDeviceInfo GetDeviceInfo(PvDeviceInfo pvDeviceInfo)
    {
        return new GcDeviceInfo(
            vendorName: pvDeviceInfo.VendorName,
            modelName: pvDeviceInfo.ModelName,
            serialNumber: pvDeviceInfo.SerialNumber,
            uniqueID: pvDeviceInfo.UniqueID.Replace(":", string.Empty),
            userDefinedName: pvDeviceInfo.UserDefinedName,
            deviceClass: DeviceClassInfo,
            isAccessible: pvDeviceInfo.IsConfigurationValid);
    }

    #endregion

    #region IDeviceEnumerator

    /// <summary>
    /// Enumerates and returns a list of available devices of type <see cref="PvCam"/>.
    /// </summary>
    /// <returns>List of available devices.</returns>
    public static List<GcDeviceInfo> EnumerateDevices()
    {
        var deviceList = new List<GcDeviceInfo>();

        // Time to search the system for devices (milliseconds).
        var pvSystem = new PvSystem { DetectionTimeout = 500 };

        // Find all available cameras on all available interfaces.
        pvSystem.Find();
        uint cameraCount = pvSystem.DeviceCount;
        for (uint i = 0; i < cameraCount; i++)
        {
            PvDeviceInfo pvDeviceInfo = pvSystem.GetDeviceInfo(i);

            // Check if license is valid.
            if (pvDeviceInfo.IsLicenseValid == false)
            {
                // PvSystem will find devices from other manufacturers...
                //Log.Error(new PvException(PvResultCode.NO_LICENSE, pvDeviceInfo.LicenseMessage), "License is not valid!");
                continue; // skip
            }

            // Check if driver is OK.
            if (pvDeviceInfo.IsConfigurationValid == false)
            {
                //Log.Error(new PvException(PvResultCode.GENERIC_ERROR, "Invalid configuration. Check that Pleora driver is properly installed."), "Configuration is not valid!");
                continue; // skip
            }

            deviceList.Add(GetDeviceInfo(pvDeviceInfo));
        }

        return deviceList;
    }

    #endregion

    #region IDeviceClassDescriptor

    /// <inheritdoc/>
    public static GcDeviceClassInfo DeviceClassInfo { get; } = new GcDeviceClassInfo("eBUS SDK", FileHelper.GetAssemblyFileVersion("PvDotNet.dll"), typeof(PvCam));

    #endregion
}