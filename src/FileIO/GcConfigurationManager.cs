using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using GcLib.Utilities.Collections;

namespace GcLib;

/// <summary>
/// Manages loading and saving of configuration xml files, containing lists of simple camera parameter name/value string pairs (properties).
/// </summary>
public sealed class GcConfigurationManager : IDisposable
{
    #region Fields

    /// <summary>
    /// Camera device.
    /// </summary>
    private GcDevice _camera;

    /// <summary>
    /// List of properties, containing simple parameter name/value string pairs.
    /// </summary>
    private List<KeyValuePair<string, string>> _propertyList;

    #endregion

    #region Properties

    /// <summary>
    /// Number of parameter name/value string pairs (properties).
    /// </summary>
    public uint PropertyCount => (uint)_propertyList.Count;

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
    public GcConfigurationManager(GcDevice camera)
    {
        _camera = camera;
        DeviceName = _camera.DeviceInfo.UniqueID;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Imports a list of properties from an xml configuration file.
    /// </summary>
    /// <param name="filePath">Filepath to configuration xml file.</param>
    public void Load(string filePath)
    {
        var propertyList = new List<KeyValuePair<string, string>>();

        using var reader = XmlReader.Create(filePath);

        //reader.ReadStartElement("GcLib:ParameterList");
        reader.ReadToFollowing("ParameterList");
        if (reader.GetAttribute("Device") != DeviceName)
        {
            throw new InvalidOperationException($"File {filePath} is incompatible with device {DeviceName}!");
        }

        _ = reader.Read();
        while (reader.NodeType != XmlNodeType.EndElement)
        {
            if (reader.NodeType == XmlNodeType.Whitespace)
            {
                _ = reader.Read();
                continue;
            }
            string key = reader.GetAttribute("Name");
            string value = reader.GetAttribute("Value");

            propertyList.Add(new KeyValuePair<string, string>(key, value));

            _ = reader.Read();
        }

        _propertyList = propertyList;
    }

    /// <summary>
    /// Exports a list of properties to an xml configuration file.
    /// </summary>
    /// <param name="filePath">Filepath to configuration xml file.</param>
    public void Save(string filePath)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            NewLineOnAttributes = true,
        };

        using var writer = XmlWriter.Create(filePath, settings);

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

        // root
        writer.WriteEndElement();
    }

    /// <summary>
    /// Restores a configuration to a device.
    /// </summary>
    /// <param name="camera">Camera device.</param>
    public void Restore(GcDevice camera)
    {
        IReadOnlyParameterCollection parameterCollection = camera.Parameters;

        foreach (KeyValuePair<string, string> property in _propertyList)
        {
            string parameterName = property.Key;
            string parameterValue = property.Value;

            if (parameterCollection.IsImplemented(parameterName))
            {
                GcParameter gcParameter = camera.Parameters.GetParameter(parameterName);

                // Only change if needed.
                if (camera.Parameters.GetParameterValue(parameterName) == parameterValue)
                    continue;

                if (gcParameter.IsImplemented && gcParameter.IsWritable)
                {
                    try
                    {
                        camera.Parameters.SetParameterValue(parameterName, parameterValue);
                    }
                    catch (Exception)
                    {
                        // skip
                    }
                }
            }
        }
    }

    /// <summary>
    /// Stores a list of simple parameter name/value string pairs from a camera parameter collection.
    /// </summary>
    /// <param name="parameterList">Camera parameter list.</param>
    public void Store(IReadOnlyList<GcParameter> parameterList)
    {
        var propertyList = new List<KeyValuePair<string, string>>();

        foreach (GcParameter gcParameter in parameterList)
        {
            if (gcParameter is GcCommand || gcParameter.Visibility == GcVisibility.Invisible)
                continue;

            string parameterName = gcParameter.Name;
            string parameterValue = gcParameter.ToString();

            // Sorting by SelectedParameters.
            List<string> selectedParameters = gcParameter.SelectedParameters;
            int destinationIndex = propertyList.Count; // default position is at the end of the list
            if (selectedParameters != null)
            {
                foreach (string parameter in selectedParameters)
                {
                    if (propertyList.Where(keyValuePair => keyValuePair.Key == parameter).ToList().Count > 0)
                    {
                        int pos = propertyList.FindIndex(p => p.Key == parameter); // position of parameter being selected
                        if (pos < destinationIndex)
                            destinationIndex = pos;
                    }
                }
            }

            propertyList.Insert(destinationIndex, new KeyValuePair<string, string>(parameterName, parameterValue)); // insert before all selected parameters (if any)
        }

        _propertyList = propertyList;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _propertyList?.Clear();
        _camera = null;
    }

    #endregion
}