using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
using FusionViewer.Models;
using FusionViewer.ViewModels;
using GcLib;
using Serilog;

namespace FusionViewer.Utilities.IO;

/// <summary>
/// Manages storing and restoring of fusion system configurations, including device and processing settings.
/// </summary>
internal sealed class FusionConfigurationService : IConfigurationService
{
    #region Fields

    /// <summary>
    /// Controls data conformance and format of xml output.
    /// </summary>
    private readonly XmlWriterSettings _xmlWriterSettings;

    /// <summary>
    /// Controls reading settings of xml input.
    /// </summary>
    private readonly XmlReaderSettings _xmlReaderSettings;

    /// <summary>
    /// Devices where device settings are stored.
    /// </summary>
    private readonly DeviceModel[] _deviceModels;

    /// <summary>
    /// Image channels where processing settings are stored.
    /// </summary>
    private readonly ImageModel[] _imageModels;

    /// <summary>
    /// Provides device information and connections.
    /// </summary>
    private readonly IDeviceProvider _deviceProvider;

    /// <summary>
    /// Creates a new service for reading/writing fusion system configurations.
    /// </summary>
    /// <param name="deviceModels">Devices where device settings are stored.</param>
    /// <param name="imageModels">Image channels where image processing settings are stored.</param>
    /// <param name="deviceProvider">Provides access to physical devices.</param>
    public FusionConfigurationService(DeviceModel[] deviceModels, ImageModel[] imageModels, IDeviceProvider deviceProvider)
    {
        _deviceModels = deviceModels;
        _imageModels = imageModels;
        _deviceProvider = deviceProvider;

        _xmlWriterSettings = new() { Indent = true, NewLineOnAttributes = false };

        _xmlReaderSettings = new() { IgnoreComments = true, IgnoreWhitespace = true };
        _xmlReaderSettings.Schemas.Add("SystemConfiguration", @"Resources\XMLSchemas\FusionConfigurationXMLSchema.xsd");
        _xmlReaderSettings.ValidationType = ValidationType.Schema;
    }

    #endregion

    #region Public Methods

    /// <inheritdoc />
    /// <exception cref="FileFormatException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public async Task RestoreAsync(string filePath, CancellationToken token)
    {
        try
        {
            // Create reader of xml files.
            using var reader = XmlReader.Create(filePath, _xmlReaderSettings);

            if (reader.MoveToContent() != XmlNodeType.None
                && (reader.Name == "FusionViewer" && reader.NamespaceURI == "SystemConfiguration"
                && reader.Read()) == false)
                throw new FileFormatException($"File is not a configuration file!");

            // Read device information.
            List<DeviceInfo> deviceInfos = [];
            foreach (var deviceModel in _deviceModels)
            {
                if (reader.Name == nameof(DeviceInfo) && reader.GetAttribute("Device") == deviceModel.DeviceIndex.ToString())
                {
                    reader.ReadStartElement(nameof(DeviceInfo));
                    deviceInfos.Add(new DeviceInfo(VendorName: reader.ReadElementContentAsString(), ModelName: reader.ReadElementContentAsString(), UniqueID: reader.ReadElementContentAsString()));
                    reader.ReadEndElement();
                }
            }

            ValidateDeviceAvailability(deviceInfos);

            // Disconnect devices if necessary.
            foreach (var deviceModel in _deviceModels)
            {
                if (deviceModel.IsConnected)
                    await deviceModel.DisconnectDeviceAsync();

                token.ThrowIfCancellationRequested();
            }

            // Connect devices.
            for (var i = 0; i < deviceInfos.Count; i++)
            {
                await _deviceModels[i].ConnectDeviceAsync(deviceInfos[i], _deviceProvider);
                token.ThrowIfCancellationRequested();
            }

            // Restore device properties.
            foreach (var deviceModel in _deviceModels.Where(d => d.IsConnected))
            {
                if (reader.Name == "PropertyList" && reader.GetAttribute("Device") == deviceModel.DeviceIndex.ToString())
                {
                    // Read device properties.
                    reader.ReadStartElement("PropertyList");
                    deviceModel.ReadXml(reader);

                    // Update device parameters.
                    await Task.Run(deviceModel.Device.Parameters.Update, CancellationToken.None);

                    // Advance to next node.
                    reader.ReadEndElement();

                    Log.Debug("Device settings restored to {DeviceName}", deviceModel.ModelName);
                }
            }

            // Restore channel processing settings.
            foreach (var imageModel in _imageModels)
            {
                if (reader.Name == "Processing" && reader.GetAttribute(0) == imageModel.ImageChannel.ToString())
                {
                    reader.ReadStartElement("Processing");
                    imageModel.ReadXml(reader);

                    reader.ReadEndElement();

                    Log.Debug("Processing settings restored to {Channel}", imageModel.ImageChannel.ToString());
                }
            }

            reader.ReadEndElement();
        }
        catch (XmlSchemaValidationException ex)
        {
            throw new FileFormatException($"File does not conform to the specified XML Schema.", ex);
        }
        catch (XmlException ex)
        {
            throw new FileFormatException($"File contains incorrect/non-parsable XML.", ex);
        }
        catch (Exception)
        {
            throw;
        }
        
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException"></exception>
    public Task StoreAsync(string filePath)
    {
        // Create writer of xml files.
        using var _xmlWriter = XmlWriter.Create(filePath, _xmlWriterSettings);

        // Start document.
        _xmlWriter.WriteStartDocument();

        // Starts fusion system configuration root element.
        _xmlWriter.WriteStartElement("FusionViewer", "SystemConfiguration");
        _xmlWriter.WriteAttributeString("Version", MainWindowViewModel.MajorMinorVersion);

        if (_deviceModels.All(d => d.IsConnected == false))
            throw new InvalidOperationException("Configuration must contain at least one device!");

        // Store device top-level information.
        foreach (DeviceModel device in _deviceModels)
        {
            if (device.IsConnected)
            {
                _xmlWriter.WriteStartElement(nameof(DeviceInfo));
                _xmlWriter.WriteAttributeString("Device", device.DeviceIndex.ToString());
                _xmlWriter.WriteElementString(nameof(DeviceModel.VendorName), device.VendorName);
                _xmlWriter.WriteElementString(nameof(DeviceModel.ModelName), device.ModelName);
                _xmlWriter.WriteElementString(nameof(DeviceModel.UniqueID), device.UniqueID);
                _xmlWriter.WriteEndElement();
            }
        }

        // Store device settings.
        foreach (DeviceModel device in _deviceModels)
        {
            if (device.IsConnected)
            {
                _xmlWriter.WriteStartElement("PropertyList");
                _xmlWriter.WriteAttributeString("Device", device.DeviceIndex.ToString());
                device.WriteXml(_xmlWriter);
                _xmlWriter.WriteEndElement();
            }
        }

        // Store image channel processing settings.
        foreach (ImageModel image in _imageModels)
        {
            _xmlWriter.WriteStartElement("Processing");
            _xmlWriter.WriteAttributeString(nameof(image.ImageChannel), image.ImageChannel.ToString());
            image.WriteXml(_xmlWriter);
            _xmlWriter.WriteEndElement();
        }

        // Close root element.
        _xmlWriter.WriteEndElement();

        // Close document.
        _xmlWriter.WriteEndDocument();

        return Task.CompletedTask;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Validates availability of devices and throws <see cref="InvalidOperationException"/> if any of the devices is not accessible.
    /// </summary>
    /// <param name="deviceInfos">Top-level info about devices.</param>
    /// <exception cref="InvalidOperationException"></exception>
    private void ValidateDeviceAvailability(List<DeviceInfo> deviceInfos)
    {
        // Update system on available devices.
        _deviceProvider.UpdateDeviceList();

        // Check device availability and build exception message if not found.
        string exceptionMessage = string.Empty;
        foreach (DeviceInfo deviceInfo in deviceInfos)
        {
            GcDeviceInfo gcDeviceInfo = _deviceProvider.GetDeviceInfo(deviceInfo.UniqueID);
            if (gcDeviceInfo is null || (gcDeviceInfo.IsOpen || gcDeviceInfo.IsAccessible) == false)
                exceptionMessage += $"\nDevice {deviceInfo.ModelName} (ID: {deviceInfo.UniqueID}) not found!";
        }

        // Raise exception if device is not found.
        if (string.IsNullOrEmpty(exceptionMessage))
            return;
        else throw new InvalidOperationException(exceptionMessage);
    }

    #endregion
}