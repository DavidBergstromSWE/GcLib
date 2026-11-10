using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
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
        /// Camera device to which the parameters belong.
        /// </summary>
        private readonly GcDevice _device;

        /// <summary>
        /// Cached collection of device parameters, implemented as a dictionary with parameter name as key and the parameter itself as value.
        /// </summary>
        /// <remarks>This is a cached collection of the device parameters, for quick parameter lookup by name. To get an updated list of parameters with new values from the device, use the Update() method.</remarks>
        private readonly Dictionary<string, GcParameter> _parameterDict;

        /// <summary>
        /// Synchronization lock for parameter dictionary access.
        /// </summary>
        private readonly Lock _parameterDictLock = new();

        #endregion

        #region Indexers

        /// <inheritdoc/>
        public GcParameter this[string parameterName]
        {
            get
            {
                lock (_parameterDictLock)
                    return _parameterDict.TryGetValue(parameterName, out GcParameter value) ? value : throw new KeyNotFoundException($"Device does not implement a parameter with name {parameterName}!");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Collection of parameter names that failed to be imported from device.
        /// </summary>
        public IReadOnlyCollection<string> FailedParameters { get; }

        /// <summary>
        /// Gets the number of parameters implemented by the device.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_parameterDictLock)
                    return _parameterDict.Count;
            }
        }

        /// <summary>
        /// Gets the name of the parameter collection. This is configured as the unique ID of the device associated with the parameters.
        /// </summary>
        public string Name { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a <see cref="GcDeviceParameterCollection"/> with the specified device, parameters, and failed
        /// parameter names.
        /// </summary>
        /// <param name="device">The device associated with the parameters.</param>
        /// <param name="parameters">List of successfully imported parameters implemented by device.</param>
        /// <param name="failedParameters">The names of parameters that failed to be imported.</param>
        public GcDeviceParameterCollection(GcDevice device, List<GcParameter> parameters, List<string> failedParameters)
        {
            ArgumentNullException.ThrowIfNull(device);
            ArgumentNullException.ThrowIfNull(parameters);
            ArgumentNullException.ThrowIfNull(failedParameters);
            ArgumentNullException.ThrowIfNull(device.DeviceInfo);

            _device = device;
            _parameterDict = new Dictionary<string, GcParameter>(parameters.ToDictionary(x => x.Name, x => x));
            FailedParameters = new List<string>(failedParameters).AsReadOnly();
            Name = _device.DeviceInfo.UniqueID;

            // Log debugging info.
            if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
                GcLibrary.Logger.LogTrace("{ParameterCount} parameters successfully imported ({FailedParameterCount} failed or skipped) from {ModelName} ({ID})", parameters.Count, FailedParameters.Count, _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves a cached list of parameters implemented by device. Use Update() method to retrieve an updated list with new values from device.
        /// </summary>
        public IReadOnlyList<GcParameter> ToList()
        {
            lock (_parameterDictLock)
                return [.. _parameterDict.Values];
        }

        /// <inheritdoc/>
        public IReadOnlyList<GcParameter> ToList(GcVisibility visibility = GcVisibility.Guru)
        {
            lock (_parameterDictLock)
                return [.. _parameterDict.Values.Where(p => p.Visibility <= visibility)];
        }

        /// <inheritdoc/>
        public IEnumerable<string> GetCategories(GcVisibility visibility)
        {
            lock (_parameterDictLock)
                return [.. _parameterDict.Values.Where(p => p.Visibility <= visibility).Select(p => p.Category).Distinct()];
        }

        /// <summary>
        /// Polls all parameters in device and refreshes collection of parameters based on current device state.
        /// </summary>
        public void Update()
        {
            // Measure elapsed time for making update.
            var startTime = Stopwatch.GetTimestamp();

            List<GcParameter> parameters;
            lock (_parameterDictLock)
                parameters = [.. _parameterDict.Values];

            // Update each parameter in collection with current value and access mode from device.
            foreach (GcParameter parameter in parameters)
            {
                try
                {
                    // Retrieve parameter value from device and update parameter in collection.
                    string parameterValue = GetParameterValue(parameter.Name);
                    if (parameterValue is not null)
                    {
                        lock (_parameterDictLock)
                            _parameterDict[parameter.Name].FromString(parameterValue);
                    }

                    // Retrieve parameter access mode from device and update mode in collection (can change on device state).
                    var param = _device.GetParameter(parameter.Name);
                    if (param != null)
                    {
                        lock (_parameterDictLock)
                            _parameterDict[parameter.Name].ImposeAccessMode(param.IsReadable, param.IsWritable); // Update access mode in collection
                    }
                }
                catch (Exception ex)
                {
                    // Log debugging info.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                        GcLibrary.Logger.LogError(ex, "Failed to update {ParameterName} in {ModelName} ({ID})", parameter.Name, _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);
                }
            }

            // Log debugging info.
            if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
                GcLibrary.Logger.LogTrace("Updated all parameters in {ModelName} ({ID}) in {ElapsedMilliseconds:000} milliseconds", _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID, Stopwatch.GetElapsedTime(startTime).TotalMilliseconds);
        }

        /// <inheritdoc/>
        public bool IsImplemented(string parameterName)
        {
            lock (_parameterDictLock)
                return _parameterDict.ContainsKey(parameterName);
        }

        /// <summary>
        /// Retrieves current parameter value from device.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        /// <returns>Parameter value as string or null if the parameter is not found, not readable, or is a command.</returns>
        public string GetParameterValue(string parameterName)
        {
            bool canGet;
            lock (_parameterDictLock)
                canGet = _parameterDict.TryGetValue(parameterName, out var parameter) && parameter.IsReadable && parameter.Type != GcParameterType.Command;
            return canGet ? _device.GetParameterValue(parameterName) : null;
        }

        /// <summary>
        /// Modifies parameter value in device.
        /// </summary>
        /// <param name="parameterName">Parameter name.</param>
        /// <param name="parameterValue">New parameter value.</param>
        public void SetParameterValue(string parameterName, string parameterValue)
        {
            bool canSet;
            lock (_parameterDictLock)
                canSet = _parameterDict.TryGetValue(parameterName, out var parameter) && parameter.IsWritable && parameter.Type != GcParameterType.Command;

            if (canSet)
            {
                _device.SetParameterValue(parameterName, parameterValue);

                if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
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
                bool canExecute;
                lock (_parameterDictLock)
                    canExecute = _parameterDict.TryGetValue(parameterName, out var parameter) && parameter.Type == GcParameterType.Command;

                if (canExecute)
                {
                    _device.ExecuteParameterCommand(parameterName);

                    if (GcLibrary.Logger.IsEnabled(LogLevel.Debug))
                        GcLibrary.Logger.LogDebug("{ParameterName} executed in {ModelName} ({ID})", parameterName, _device.DeviceInfo.ModelName, _device.DeviceInfo.UniqueID);

                    // Raise invalidation event (as other parameters may be affected).
                    _device.OnParameterInvalidate(new ParameterInvalidateEventArgs(parameterName));
                }
            }
            catch (Exception ex)
            {
                if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
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
            lock (_parameterDictLock)
            {
                if (_parameterDict.TryGetValue(parameterName, out var parameter))
                {
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
        }

        /// <summary>
        /// Returns a simplified list of parameter name/value string pairs from collection, up to the specified user visibility.
        /// </summary>
        /// <param name="visibility">Visibility level.</param>
        /// <returns>List of <i>key</i>/<i>value</i> pairs, where <i>key</i> is parameter name and <i>value</i> is parameter value.</returns>
        public IReadOnlyList<KeyValuePair<string, string>> GetPropertyList(GcVisibility visibility = GcVisibility.Guru)
        {
            var propertyList = new List<KeyValuePair<string, string>>();
            var propertyIndexLookup = new Dictionary<string, int>();

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
                        if (propertyIndexLookup.TryGetValue(parameter, out int pos) && pos < destinationIndex)
                            destinationIndex = pos;
                    }
                }

                propertyList.Insert(destinationIndex, new KeyValuePair<string, string>(parameterName, parameterValue)); // insert before all selected parameters (if any)

                // Rebuild lookup indexes from insertion point onward.
                for (int i = destinationIndex; i < propertyList.Count; i++)
                    propertyIndexLookup[propertyList[i].Key] = i;
            }

            return propertyList;
        }

        /// <inheritdoc/>
        public IEnumerator<GcParameter> GetEnumerator()
        {
            lock (_parameterDictLock)
                return _parameterDict.Values.ToList().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}