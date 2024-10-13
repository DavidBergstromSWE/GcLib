using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using FusionViewer.Utilities.Messages;
using GcLib;
using Serilog;

namespace FusionViewer.Models;

/// <summary>
/// Models a camera device used as an input acquisition channel for image fusion.
/// </summary>
internal sealed class DeviceModel : ObservableValidator, IXmlSerializable
{
    #region Fields

    // backing-fields
    private GcDevice _device;
    private bool _isConnected;
    private bool _isConnecting;
    private string _modelName;
    private string _vendorName;
    private string _uniqueID;
    private string _channelName;

    #endregion

    #region Properties

    [MinLength(1, ErrorMessage = "Alias must be at least 1 character")]
    [MaxLength(9, ErrorMessage = "Alias can not be longer than 9 characters")]
    /// <summary>
    /// Name of channel.
    /// </summary>
    public string ChannelName
    {
        get => _channelName;
        set => TrySetProperty(ref _channelName, value, out _);
    }

    /// <summary>
    /// Index of device.
    /// </summary>
    public DeviceIndex DeviceIndex { get; }

    /// <summary>
    /// Model name of device. 
    /// </summary>
    public string ModelName
    {
        get => _modelName;
        private set => SetProperty(ref _modelName, value);
    }

    /// <summary>
    /// Vendor name of device.
    /// </summary>
    public string VendorName
    {
        get => _vendorName;
        private set => SetProperty(ref _vendorName, value);
    }

    /// <summary>
    /// Unique string identifier of device.
    /// </summary>
    public string UniqueID
    {
        get => _uniqueID;
        private set => SetProperty(ref _uniqueID, value);
    }

    /// <summary>
    /// Connected device.
    /// </summary>
    public GcDevice Device
    {
        get => _device;
        private set => SetProperty(ref _device, value);
    }

    /// <summary>
    /// Indicates if device is connected.
    /// </summary>
    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    /// <summary>
    /// Indicates that device is currently being connected to.
    /// </summary>
    public bool IsConnecting
    {
        get => _isConnecting;
        set
        {
            if (SetProperty(ref _isConnecting, value))
            {
                if (_isConnecting)
                    SetDeviceInfo("Loading...");
            }
        }
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new model for a device.
    /// </summary>
    /// <param name="deviceIndex">Index for device.</param>
    /// <param name="channelName">Name of channel.</param>
    public DeviceModel(DeviceIndex deviceIndex, string channelName)
    {
        DeviceIndex = deviceIndex;
        ChannelName = channelName;
        SetDeviceInfo();
    }

    /// <summary>
    /// Instantiates a new mockup model for a device, with dummy model name, vendor name and ID.
    /// </summary>
    /// <param name="deviceIndex">Index for device.</param>
    /// <param name="channelName">Name of channel.</param>
    /// <param name="modelName">Name of model.</param>
    /// <param name="vendorName">Name of vendor.</param>
    /// <param name="uniqueID">Device ID.</param>
    public DeviceModel(DeviceIndex deviceIndex, string channelName, string modelName, string vendorName, string uniqueID)
    {
        DeviceIndex = deviceIndex;
        ChannelName = channelName;
        SetDeviceInfo(modelName, vendorName, uniqueID);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Connects to a device using device information.
    /// </summary>
    /// <param name="deviceInfo">Top-level info about device.</param>
    /// <param name="deviceProvider">Provider of devices.</param>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task ConnectDeviceAsync(DeviceInfo deviceInfo, IDeviceProvider deviceProvider)
    {
        IsConnecting = true;

        Log.Debug("Attempting to connect device {Device} (ID: {ID}) to {Channel}", deviceInfo.ModelName, deviceInfo.UniqueID, ChannelName);

        try
        {
            // Try to connect to device (and wait for task completion).
            Device = await Task.Run(() => deviceProvider.OpenDevice(deviceInfo.UniqueID));

            // Update device info.
            SetDeviceInfo(deviceInfo.ModelName, deviceInfo.VendorName, deviceInfo.UniqueID);

            IsConnected = true;

            // Announce connection event.
            _ = WeakReferenceMessenger.Default.Send(new DeviceConnectedMessage(DeviceIndex));

            // Hook eventhandler to lost connection events.
            Device.ConnectionLost += OnConnectionLost;

            // Log information.
            Log.Information("Device {Device} (ID: {ID}) connected to {Channel}", deviceInfo.ModelName, deviceInfo.UniqueID, ChannelName);
        }
        catch (TargetInvocationException ex)
        {
            Log.Error(ex.InnerException, "Error when attempting to connect to Device {Device} (ID: {ID})", deviceInfo.ModelName, deviceInfo.UniqueID);
            throw ex.InnerException;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error when attempting to connect to Device {Device} (ID: {ID})", deviceInfo.ModelName, deviceInfo.UniqueID);
            throw;
        }
        finally
        {
            IsConnecting = false;

            // Update device info.
            if (Device == null)
            {
                SetDeviceInfo("No camera connected");
                IsConnected = false;
            }
        }
    }

    /// <summary>
    /// Disconnects the device currently connected.
    /// </summary>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task DisconnectDeviceAsync()
    {
        if (IsConnected == false)
            throw new InvalidOperationException($"No device is connected to {ChannelName}!");

        // Close device (and wait for task completion).
        await Task.Run(Device.Close);

        // Unhook eventhandler to lost connection events.
        Device.ConnectionLost -= OnConnectionLost;

        // Announce disconnection event.
        _ = WeakReferenceMessenger.Default.Send(new DeviceDisconnectedMessage(DeviceIndex));

        // Log information.
        Log.Information("Device {Device} (ID: {ID}) disconnected from {Channel}", ModelName, UniqueID, ChannelName);

        // Update device info.
        Device = null;
        SetDeviceInfo("No camera connected");
        IsConnected = false;
    }

    /// <summary>
    /// Updates the device with the most current parameters read from device.
    /// </summary>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Task UpdateDeviceAsync()
    {
        if (IsConnected == false)
            throw new InvalidOperationException($"No device is connected to {ChannelName}!");

        // Update parameter collection of device with current parameter values (and wait for task completion).
        return Task.Run(Device.Parameters.Update);
    }

    #endregion

    #region IXmlSerializable

    /// <inheritdoc/>
    public XmlSchema GetSchema()
    {
        return null;
    }

    /// <inheritdoc/>
    public void ReadXml(XmlReader reader)
    {
        while (reader.NodeType != XmlNodeType.EndElement && reader.NodeType != XmlNodeType.None)
        {
            if (reader.IsEmptyElement == false)
            {
                // Read parameter name and value from xml file.
                string parameterName = reader.Name;
                string parameterValue = reader.ReadElementContentAsString();

                // Modify parameter in device.
                if (Device.Parameters.IsImplemented(parameterName))
                {
                    // Retrieve parameter from device.
                    GcParameter gcParameter = Device.Parameters.GetParameter(parameterName);

                    // Only modify if parameter is writable and if change is needed.
                    if (gcParameter.IsWritable && parameterValue != gcParameter.ToString())
                    {
                        try
                        {
                            // Try to set parameter in device to value read from file.
                            Device.Parameters.SetParameterValue(parameterName, parameterValue);
                        }
                        catch (Exception ex)
                        {
                            // Log exception if raised.
                            Log.Error(ex, "Parameter {Name} could not be set to {Value} in device {model} (ID: {id})", parameterName, parameterValue, ModelName, UniqueID);
                        }
                    }
                }
            }
            else
            {
                _ = reader.Read();
            }
        }
    }

    /// <inheritdoc/>
    public void WriteXml(XmlWriter writer)
    {
        // Retrieve list of parameter names and values from device.
        var propertyList = Device.Parameters.GetPropertyList();

        // Write parameter data to xml file.
        foreach (KeyValuePair<string, string> property in propertyList)
            writer.WriteElementString(property.Key, property.Value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Event announcing that connection to device was lost.
    /// </summary>
    public event EventHandler ConnectionLost;

    /// <summary>
    /// Event-invoking method, announcing that connection to device was lost.
    /// </summary>
    private void OnConnectionLost(object sender, EventArgs eventArgs)
    {
        ConnectionLost?.Invoke(this, eventArgs);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Set device information to be displayed in UI.
    /// </summary>
    /// <param name="modelName">Model name.</param>
    /// /// <param name="vendorName">Vendor name.</param>
    /// <param name="uniqueID">Unique ID.</param>
    private void SetDeviceInfo(string modelName, string vendorName, string uniqueID)
    {
        ModelName = modelName;
        VendorName = vendorName;
        UniqueID = uniqueID;
    }

    /// <summary>
    /// Set device information to be displayed in UI.
    /// </summary>
    /// <param name="message">Message to display.</param>
    private void SetDeviceInfo(string message = "No camera connected")
    {
        ModelName = VendorName = UniqueID = message;
    }

    #endregion
}