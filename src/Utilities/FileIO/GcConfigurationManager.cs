using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using GcLib.Utilities.Collections;

namespace GcLib.Utilities.FileIO;

/// <summary>
/// Manages loading and saving of configuration xml files, containing lists of simple camera parameter name/value string pairs (properties).
/// </summary>
public sealed class GcConfigurationManager : IDisposable
{
    #region Fields

    /// <summary>
    /// List of properties, containing simple parameter name/value string pairs. Null until properties have been loaded or stored.
    /// </summary>
    private List<KeyValuePair<string, string>> _propertyList;

    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>
    /// Number of parameter name/value string pairs (properties). Zero if no properties have been loaded or stored.
    /// </summary>
    public uint PropertyCount => (uint)(_propertyList?.Count ?? 0);

    /// <summary>
    /// Name of device.
    /// </summary>
    public string DeviceName { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new manager for loading and saving camera configuration xml files.
    /// </summary>
    /// <param name="camera">Camera device.</param>
    /// <exception cref="ArgumentNullException"><paramref name="camera"/> is null.</exception>
    public GcConfigurationManager(GcDevice camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        DeviceName = camera.DeviceInfo.UniqueID;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Imports a list of properties from an xml configuration file.
    /// </summary>
    /// <param name="filePath">Filepath to configuration xml file.</param>
    /// <exception cref="InvalidDataException">The file is not a valid configuration file.</exception>
    /// <exception cref="InvalidOperationException">The file was created for another device.</exception>
    /// <exception cref="XmlException">The file contains malformed xml.</exception>
    public void Load(string filePath)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var propertyList = new List<KeyValuePair<string, string>>();

        var settings = new XmlReaderSettings
        {
            IgnoreComments = true,
            IgnoreProcessingInstructions = true,
            IgnoreWhitespace = true,
        };

        using var reader = XmlReader.Create(filePath, settings);

        if (!reader.ReadToFollowing("ParameterList"))
        {
            throw new InvalidDataException($"File {filePath} is not a configuration file (missing ParameterList element)!");
        }

        if (reader.GetAttribute("Device") != DeviceName)
        {
            throw new InvalidOperationException($"File {filePath} is incompatible with device {DeviceName}!");
        }

        // An empty list is written as a self-closing element, with nothing to read.
        if (!reader.IsEmptyElement)
        {
            int rootDepth = reader.Depth;
            while (reader.Read() && !(reader.NodeType == XmlNodeType.EndElement && reader.Depth == rootDepth))
            {
                if (reader.NodeType != XmlNodeType.Element || reader.Name != "Parameter")
                    continue;

                string key = reader.GetAttribute("Name");
                string value = reader.GetAttribute("Value");

                if (string.IsNullOrEmpty(key) || value is null)
                {
                    throw new InvalidDataException($"File {filePath} contains a parameter without Name or Value attribute (line {(reader as IXmlLineInfo)?.LineNumber})!");
                }

                propertyList.Add(new KeyValuePair<string, string>(key, value));
            }
        }

        _propertyList = propertyList;
    }

    /// <summary>
    /// Exports a list of properties to an xml configuration file.
    /// </summary>
    /// <param name="filePath">Filepath to configuration xml file.</param>
    /// <exception cref="InvalidOperationException">No properties have been loaded or stored.</exception>
    public void Save(string filePath)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrEmpty(filePath);
        if (_propertyList is null)
            throw new InvalidOperationException("There are no properties to save. Call Store or Load first!");

        var settings = new XmlWriterSettings
        {
            Indent = true,
            NewLineOnAttributes = true,
        };

        // Written to a temporary file first, so that a failure never leaves a partial configuration file behind.
        string tempPath = filePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var writer = XmlWriter.Create(tempPath, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("ParameterList"); // root element
                writer.WriteAttributeString("Device", DeviceName);

                foreach (KeyValuePair<string, string> property in _propertyList)
                {
                    writer.WriteStartElement("Parameter");
                    writer.WriteAttributeString("Name", property.Key);
                    writer.WriteAttributeString("Value", property.Value);
                    writer.WriteEndElement();
                }

                writer.WriteEndElement(); // root
                writer.WriteEndDocument();
            }

            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            try { File.Delete(tempPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            throw;
        }
    }

    /// <summary>
    /// Restores a configuration to a device. Parameters that are not implemented by the device, not writable or already have the stored value are left untouched.
    /// </summary>
    /// <param name="camera">Camera device.</param>
    /// <returns>Names of parameters that could not be restored (empty if all succeeded).</returns>
    /// <exception cref="InvalidOperationException">No properties have been loaded or stored, or the device is not the one the configuration was created for.</exception>
    public IReadOnlyList<string> Restore(GcDevice camera)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(camera);
        if (_propertyList is null)
            throw new InvalidOperationException("There are no properties to restore. Call Load or Store first!");
        if (camera.DeviceInfo.UniqueID != DeviceName)
            throw new InvalidOperationException($"Configuration is incompatible with device {camera.DeviceInfo.UniqueID}!");

        IReadOnlyParameterCollection parameterCollection = camera.Parameters;
        var failedParameters = new List<string>();

        foreach (KeyValuePair<string, string> property in _propertyList)
        {
            string parameterName = property.Key;
            string parameterValue = property.Value;

            if (!parameterCollection.IsImplemented(parameterName))
                continue;

            try
            {
                GcParameter gcParameter = parameterCollection.GetParameter(parameterName);
                if (!gcParameter.IsImplemented || !gcParameter.IsWritable)
                    continue;

                // Only change if needed.
                if (gcParameter.IsReadable && parameterCollection.GetParameterValue(parameterName) == parameterValue)
                    continue;

                parameterCollection.SetParameterValue(parameterName, parameterValue);
            }
            catch (Exception)
            {
                failedParameters.Add(parameterName);
            }
        }

        return failedParameters;
    }

    /// <summary>
    /// Stores a list of simple parameter name/value string pairs from a camera parameter collection.
    /// </summary>
    /// <param name="parameterList">Camera parameter list.</param>
    public void Store(IReadOnlyList<GcParameter> parameterList)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameterList);

        var propertyList = new List<KeyValuePair<string, string>>();

        foreach (GcParameter gcParameter in parameterList)
        {
            if (gcParameter is GcCommand || gcParameter.Visibility == GcVisibility.Invisible || !gcParameter.IsImplemented)
                continue;

            string parameterName = gcParameter.Name;
            string parameterValue = gcParameter.ToString();

            // Sorting by SelectedParameters.
            IReadOnlyList<string> selectedParameters = gcParameter.SelectedParameters;
            int destinationIndex = propertyList.Count; // default position is at the end of the list
            if (selectedParameters != null)
            {
                foreach (string parameter in selectedParameters)
                {
                    int pos = propertyList.FindIndex(p => p.Key == parameter); // position of parameter being selected
                    if (pos >= 0 && pos < destinationIndex)
                        destinationIndex = pos;
                }
            }

            propertyList.Insert(destinationIndex, new KeyValuePair<string, string>(parameterName, parameterValue)); // insert before all selected parameters (if any)
        }

        _propertyList = propertyList;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _disposed = true;
        _propertyList = null;
    }

    #endregion

    #region Private methods

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    #endregion
}