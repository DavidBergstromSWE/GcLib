using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GcLib;

public sealed partial class VirtualCam
{
    private sealed partial class GenApi
    {
        #region Backing fields

        private GcString _deviceVendorName;
        private GcString _deviceModelName;
        private GcString _deviceSerialNumber;
        private GcString _deviceUserID;
        private GcEnumeration _deviceTLType;
        private GcEnumeration _deviceTemperatureSelector;
        private GcFloat _deviceTemperature;
        private GcInteger _timeStamp;
        private GcCommand _timeStampReset;

        #endregion

        // Provides properties for device control.

        #region Device Control

        /// <summary>
        /// Name of device vendor.
        /// </summary>
        [Category("DeviceControl")]
        public GcString DeviceVendorName => _deviceVendorName;

        /// <summary>
        /// Name of device model.
        /// </summary>
        [Category("DeviceControl")]
        public GcString DeviceModelName => _deviceModelName;

        /// <summary>
        /// Serial number of device.
        /// </summary>
        [Category("DeviceControl")]
        public GcString DeviceSerialNumber => _deviceSerialNumber;

        /// <summary>
        /// User-specifiable device ID.
        /// </summary>
        [Category("DeviceControl")]
        [DefaultValue("CameraSimulator")]
        public GcString DeviceUserID => _deviceUserID;

        /// <summary>
        /// Transport layer type.
        /// </summary>
        [Category("DeviceControl")]
        public GcEnumeration DeviceTLType => _deviceTLType;

        /// <summary>
        /// Selects which device temperature to measure in <see cref="DeviceTemperature"/>.
        /// </summary>
        [Category("DeviceControl")]
        [DefaultValue(GcLib.DeviceTemperatureSelector.DeviceSpecific)]
        public GcEnumeration DeviceTemperatureSelector => _deviceTemperatureSelector;

        /// <summary>
        /// Temperature as given by device sensor selected by <see cref="DeviceTemperatureSelector"/>.
        /// </summary>
        [Category("DeviceControl")]
        [DefaultValue(20.0)]
        public GcFloat DeviceTemperature => _deviceTemperature;

        /// <summary>
        /// Provides a command to reset device to initial values.
        /// </summary>
        [Category("DeviceControl")]
        public GcCommand DeviceReset
        {
            get => new(
                name: "DeviceReset",
                category: "DeviceControl",
                description: "Resets the device to its power up state",
                method: () =>
                {
                    // Stop acquisition (if running).
                    if (_virtualCam.IsAcquiring)
                        AcquisitionStop.Execute();

                    // Restart stop watch.
                    _cameraClock.Restart();

                    // Reset and synchronize timestamp to PC time.
                    _time0 = DateTime.Now;

                    // Reset frame counter.
                    _frameCounter = 0;

                    // Reset to initial parameter values.
                    foreach (var parameter in _virtualCam.Parameters.Where(p => p.IsWritable))
                    {
                        DefaultValueAttribute attribute = TypeDescriptor.GetProperties(this)[parameter.Name].Attributes.OfType<DefaultValueAttribute>().Single();
                        _virtualCam.Parameters.SetParameterValue(parameter.Name, attribute.Value.ToString());
                    }
                },
                visibility: GcVisibility.Guru,
                isSelector: true,
                selectedParameters: [.. GetParameterNames()]); // "all"?
        }

        /// <summary>
        /// Current timestamp in device.
        /// </summary>
        [Category("DeviceControl")]
        public GcInteger TimeStamp
        {
            get
            {
                // Putting this logic in OnParameterChanged prevents it from being updated during an GcDeviceParameterCollection.Update() run...need special case for parameters updating constantly?

                // Update timestamp with current time.
                _timeStamp.Value = (long)Math.Round(_cameraClock.ElapsedTicks / (double)Stopwatch.Frequency * 10000000.0 + _time0.Ticks, 0);
                return _timeStamp;
            }
        }

        /// <summary>
        /// Provides a command to reset the timestamp in the device.
        /// </summary>
        [Category("DeviceControl")]
        public GcCommand TimestampReset => _timeStampReset;

        #endregion

        #region Private methods

        /// <summary>
        /// Returns a complete list of all parameter names.
        /// </summary>
        /// <returns>List of parameter names.</returns>
        private List<string> GetParameterNames()
        {
            List<string> parameters = [];

            // Get properties (using Reflection).
            PropertyInfo[] propertyInfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            // Add GcParameter type properties to parameterList.
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.PropertyType.BaseType == typeof(GcParameter))
                    parameters.Add(propertyInfo.Name);
            }

            return parameters;
        }

        #endregion
    }
}