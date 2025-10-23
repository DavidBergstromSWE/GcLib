using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using GcLib.Utilities.Collections;
using Microsoft.Extensions.Logging;

namespace GcLib;

public abstract partial class GcDevice : IBufferProducer
{
    /// <summary>
    /// Nested class for storing and handling device parameters.
    /// </summary>
    protected sealed class GcDeviceParameterCollection : IReadOnlyParameterCollection
    {
        #region Fields

        /// <summary>
        /// Camera device (reference to outer class object).
        /// </summary>
        private readonly GcDevice _device;

        /// <summary>
        /// Dictionary containing parameter name as key and parameter itself as value.
        /// </summary>
        private readonly Dictionary<string, GcParameter> _parameterDict;

        #endregion

        #region Indexers

        /// <inheritdoc/>
        public GcParameter this[int i] => _parameterDict.ElementAt(i).Value;

        /// <inheritdoc/>
        public GcParameter this[string parameterName] => _parameterDict.TryGetValue(parameterName, out GcParameter value) ? value : throw new KeyNotFoundException($"Device does not implement a parameter with name {parameterName}!");

        #endregion

        #region Properties

        /// <summary>
        /// Collection of parameter names that failed to be imported from device.
        /// </summary>
        public IReadOnlyCollection<string> FailedParameters { get; }

        /// <summary>
        /// Gets the number of parameters implemented by the device.
        /// </summary>
        public int Count => _parameterDict.Count;

        /// <inheritdoc/>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Returns a collection of parameters and features implemented by device.
        /// </summary>
        /// <param name="device">Camera device.</param>
        /// <param name="parameters">List of successfully imported parameters implemented by device.</param>
        /// <param name="failedParameters">List of parameter names failed to be imported.</param>
        public GcDeviceParameterCollection(GcDevice device, List<GcParameter> parameters, List<string> failedParameters)
        {
            _device = device;
            _parameterDict = new Dictionary<string, GcParameter>(parameters.ToDictionary(x => x.Name, x => x));
            FailedParameters = failedParameters;
            Name = _device.DeviceInfo.UniqueID;

            // Log debugging info.
            GcLibrary.Logger.LogTrace("{ParameterCount} parameters successfully imported ({FailedParameterCount} failed) from {ModelName} ({ID})", parameters.Count, FailedParameters.Count, _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves a cached list of parameters implemented by device. Use Update() method to retrieve an updated list with new values from device.
        /// </summary>
        public IReadOnlyList<GcParameter> ToList()
        {
            return [.. _parameterDict.Values];
        }

        /// <inheritdoc/>
        public IReadOnlyList<GcParameter> ToList(GcVisibility visibility = GcVisibility.Guru)
        {
            return [.. _parameterDict.Values.Where(p => p.Visibility <= visibility)];
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetCategories(GcVisibility visibility)
        {
            return _parameterDict.Values.Where(p => p.Visibility <= visibility).Select(p => p.Category).Distinct();
        }

        /// <summary>
        /// Polls all parameters in device and refreshes collection of parameters based on current device state.
        /// </summary>
        public void Update()
        {
            // Measure elapsed time for making update.
            var startTime = Stopwatch.GetTimestamp();

            var parameterDict = new Dictionary<string, GcParameter>(_parameterDict);
            foreach (GcParameter parameter in parameterDict.Values)
            {
                try
                {
                    _parameterDict[parameter.Name].FromString(GetParameterValue(parameter.Name));

                    var param = _device.GetParameter(parameter.Name);
                    if (param != null)
                        _parameterDict[parameter.Name].ImposeAccessMode(param.IsReadable, param.IsWritable);
                }
                catch (Exception ex)
                {
                    // Log debugging info.
                    GcLibrary.Logger.LogError(ex, "Failed to update {ParameterName} in {ModelName} ({ID})", parameter.Name, _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);
                }
            }

            // Log debugging info.             
            GcLibrary.Logger.LogTrace("Updated all parameters in {ModelName} ({ID}) in {ElapsedMilliseconds:000} milliseconds", _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID, Stopwatch.GetElapsedTime(startTime).Milliseconds);
        }

        /// <inheritdoc/>
        public bool IsImplemented(string parameterName)
        {
            return _parameterDict.ContainsKey(parameterName);
        }

        /// <summary>
        /// Retrieves current parameter value from device.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        /// <returns>Parameter value as string.</returns>
        public string GetParameterValue(string parameterName)
        {
            if (IsImplemented(parameterName) && _parameterDict[parameterName].IsReadable && _parameterDict[parameterName].Type != GcParameterType.Command)
                return _device.GetParameterValue(parameterName);
            else return null;
        }

        /// <summary>
        /// Modifies parameter value in device.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        /// <param name="parameterValue">New parameter value.</param>
        public void SetParameterValue(string parameterName, string parameterValue)
        {
            if (IsImplemented(parameterName) && _parameterDict[parameterName].IsWritable && _parameterDict[parameterName].Type != GcParameterType.Command)
            {
                _device.SetParameterValue(parameterName, parameterValue);

                GcLibrary.Logger.LogDebug("{ParameterName} set to {Value} in {ModelName} ({ID})", parameterName, GetParameterValue(parameterName), _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);

                // Raise invalidation event (as other parameters may be affected).
                _device.OnParameterInvalidate(new ParameterInvalidateEventArgs(parameterName));
            }
        }

        /// <summary>
        /// Executes parameter command in device.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        public void ExecuteParameterCommand(string parameterName)
        {
            try
            {
                if (IsImplemented(parameterName) && _parameterDict[parameterName].Type == GcParameterType.Command)
                {
                    _device.ExecuteParameterCommand(parameterName);

                    GcLibrary.Logger.LogDebug("{ParameterName} executed in {ModelName} ({ID})", parameterName, _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);

                    // Raise invalidation event (as other parameters may be affected).
                    _device.OnParameterInvalidate(new ParameterInvalidateEventArgs(parameterName));
                }
            }
            catch (Exception ex)
            {
                GcLibrary.Logger.LogError(ex, "Failed to execute {ParameterName} in {ModelName} ({ID})", parameterName, _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);
                throw;
            }
        }

        /// <summary>
        /// Retrieves parameter casted in the corresponding primitive data type.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        /// <returns>Parameter (or null if parameter is not found).</returns>
        public GcParameter GetParameter(string parameterName)
        {
            if (IsImplemented(parameterName))
            {
                var parameter = _parameterDict[parameterName];
                return parameter.Type switch
                {
                    GcParameterType.Integer => parameter as GcInteger,
                    GcParameterType.Float => parameter as GcFloat,
                    GcParameterType.String => parameter as GcString,
                    GcParameterType.Enumeration => parameter as GcEnumeration,
                    GcParameterType.Boolean => parameter as GcBoolean,
                    GcParameterType.Command => parameter as GcCommand,
                    _ => null,
                };
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Returns a simplified list of parameter name/value string pairs from collection, up to the specified user visibility.
        /// </summary>
        /// <param name="visibility">Visibility level.</param>
        /// <returns>List of <i>key</i>/<i>value</i> pairs, where <i>key</i> is parameter name and <i>value</i> is parameter value.</returns>
        public IReadOnlyList<KeyValuePair<string, string>> GetPropertyList(GcVisibility visibility = GcVisibility.Guru)
        {
            var propertyList = new List<KeyValuePair<string, string>>();

            foreach (GcParameter gcParameter in this)
            {
                if (gcParameter is GcCommand || gcParameter.Visibility > visibility)
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

            return propertyList;
        }

        /// <inheritdoc/>
        public IEnumerator<GcParameter> GetEnumerator()
        {
            return _parameterDict.Values.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}