using System;
using System.IO;
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
/// Manages storing and restoring of system configurations, including device and processing settings.
/// </summary>
internal sealed class ConfigurationService : IConfigurationService
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
    /// Device where device settings is stored.
    /// </summary>
    private readonly DeviceModel _device;

    /// <summary>
    /// Image channels where processing settings are stored.
    /// </summary>
    private readonly ImageModel _imageProcessing;

    /// <summary>
    /// Provides device information and connections.
    /// </summary>
    private readonly IDeviceProvider _deviceProvider;

    /// <summary>
    /// Creates a new service for reading/writing system configurations.
    /// </summary>
    /// <param name="deviceModels">Devices where device settings are stored.</param>
    /// <param name="imageModels">Image channels where image processing settings are stored.</param>
    /// <param name="deviceProvider">Provides access to physical devices.</param>
    public ConfigurationService(DeviceModel device, ImageModel imageModel, IDeviceProvider deviceProvider)
    {
        _device = device;
        _imageProcessing = imageModel;
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
            DeviceInfo deviceInfo = new();
            if (reader.Name == nameof(DeviceInfo))
            {
                reader.ReadStartElement(nameof(DeviceInfo));
                deviceInfo = new DeviceInfo(VendorName: reader.ReadElementContentAsString(), ModelName: reader.ReadElementContentAsString(), UniqueID: reader.ReadElementContentAsString());
                reader.ReadEndElement();
            }

            ValidateDeviceAvailability(deviceInfo);

            // Disconnect device if necessary.
            if (_device.IsConnected)
                await _device.DisconnectDeviceAsync();

                token.ThrowIfCancellationRequested();

            // Connect device.
            await _device.ConnectDeviceAsync(deviceInfo, _deviceProvider);
            token.ThrowIfCancellationRequested();

            // Restore device properties.
            if (_device.IsConnected)
            {
                if (reader.Name == "PropertyList")
                {
                    // Read device properties.
                    reader.ReadStartElement("PropertyList");
                    _device.ReadXml(reader);

                    // Update device parameters.
                    await Task.Run(_device.Device.Parameters.Update, CancellationToken.None);

                    // Advance to next node.
                    reader.ReadEndElement();

                    Log.Debug("Device settings restored to {DeviceName}", _device.ModelName);
                }
            }

            // Restore channel processing settings.
                if (reader.Name == "Processing")
                {
                    reader.ReadStartElement("Processing");
                    _imageProcessing.ReadXml(reader);

                    reader.ReadEndElement();

                    Log.Debug("Processing settings restored");
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

        // Starts system configuration root element.
        _xmlWriter.WriteStartElement("FusionViewer", "SystemConfiguration");
        _xmlWriter.WriteAttributeString("Version", MainWindowViewModel.MajorMinorVersion);

        if (_device.IsConnected == false)
            throw new InvalidOperationException("Configuration must contain at least one device!");

        // Store device top-level information.
        if (_device.IsConnected)
        {
            _xmlWriter.WriteStartElement(nameof(DeviceInfo));
            _xmlWriter.WriteElementString(nameof(DeviceModel.VendorName), _device.VendorName);
            _xmlWriter.WriteElementString(nameof(DeviceModel.ModelName), _device.ModelName);
            _xmlWriter.WriteElementString(nameof(DeviceModel.UniqueID), _device.UniqueID);
            _xmlWriter.WriteEndElement();
        }

        // Store device settings.
        if (_device.IsConnected)
        {
            _xmlWriter.WriteStartElement("PropertyList");
            _device.WriteXml(_xmlWriter);
            _xmlWriter.WriteEndElement();
        }

        // Store image channel processing settings.
        _xmlWriter.WriteStartElement("Processing");
        _imageProcessing.WriteXml(_xmlWriter);
        _xmlWriter.WriteEndElement();

        // Close root element.
        _xmlWriter.WriteEndElement();

        // Close document.
        _xmlWriter.WriteEndDocument();

        return Task.CompletedTask;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Validates availability of device and throws <see cref="InvalidOperationException"/> if device is not accessible.
    /// </summary>
    /// <param name="deviceInfo">Top-level info about device.</param>
    /// <exception cref="InvalidOperationException"></exception>
    private void ValidateDeviceAvailability(DeviceInfo deviceInfo)
    {
        // Update system on available devices.
        _deviceProvider.UpdateDeviceList();

        // Check device availability and build exception message if not found.
        string exceptionMessage = string.Empty;
        GcDeviceInfo gcDeviceInfo = _deviceProvider.GetDeviceInfo(deviceInfo.UniqueID);
        if (gcDeviceInfo is null || (gcDeviceInfo.IsOpen || gcDeviceInfo.IsAccessible) == false)
            exceptionMessage += $"\nDevice {deviceInfo.ModelName} (ID: {deviceInfo.UniqueID}) not found!";


        // Raise exception if device is not found.
        if (string.IsNullOrEmpty(exceptionMessage))
            return;
        else throw new InvalidOperationException(exceptionMessage);
    }

    #endregion
}