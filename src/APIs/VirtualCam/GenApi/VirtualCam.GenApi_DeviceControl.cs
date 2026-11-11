using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

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
        public GcString DeviceVendorName => _deviceVendorName ??= new GcString(
            name: nameof(DeviceVendorName),
            category: GetCategory(nameof(DeviceVendorName)),
            value: _virtualCam.DeviceInfo.VendorName,
            description: "Name of the manufacturer of the device",
            maxLength: 20,
            isWritable: false);

        /// <summary>
        /// Name of device model.
        /// </summary>
        [Category("DeviceControl")]
        public GcString DeviceModelName => _deviceModelName ??= new GcString(
            name: nameof(DeviceModelName),
            category: GetCategory(nameof(DeviceModelName)),
            value: _virtualCam.DeviceInfo.ModelName,
            description: "Model of the device",
            maxLength: 20,
            isWritable: false);

        /// <summary>
        /// Serial number of device.
        /// </summary>
        [Category("DeviceControl")]
        public GcString DeviceSerialNumber => _deviceSerialNumber ??= new GcString(
            name: nameof(DeviceSerialNumber),
            category: GetCategory(nameof(DeviceSerialNumber)),
            value: _virtualCam.DeviceInfo.SerialNumber,
            description: "Device's serial number",
            maxLength: 20,
            isWritable: false,
            visibility: GcVisibility.Expert);

        /// <summary>
        /// User-specifiable device ID.
        /// </summary>
        [Category("DeviceControl")]
        [DefaultValue("CameraSimulator")]
        public GcString DeviceUserID => _deviceUserID ??= new GcString(
            name: nameof(DeviceUserID),
            category: GetCategory(nameof(DeviceUserID)),
            value: "CameraSimulator",
            description: "User-programmable device identifier",
            maxLength: 20);

        /// <summary>
        /// Selects which device temperature to measure in <see cref="DeviceTemperature"/>.
        /// </summary>
        [Category("DeviceControl")]
        [DefaultValue(GcLib.DeviceTemperatureSelector.DeviceSpecific)]
        public GcEnumeration DeviceTemperatureSelector => _deviceTemperatureSelector ??= new GcEnumeration(
            name: nameof(DeviceTemperatureSelector),
            category: GetCategory(nameof(DeviceTemperatureSelector)),
            description: "Selects the location within the device, where the temperature will be measured",
            enumEntry: GcLib.DeviceTemperatureSelector.DeviceSpecific,
            enumType: typeof(DeviceTemperatureSelector),
            visibility: GcVisibility.Expert,
            isSelector: true,
            selectedParameters: [nameof(DeviceTemperature)]);

        /// <summary>
        /// Temperature as given by device sensor selected by <see cref="DeviceTemperatureSelector"/>.
        /// </summary>
        [Category("DeviceControl")]
        [DefaultValue(20.0)]
        public GcFloat DeviceTemperature => _deviceTemperature ??= new GcFloat(
            name: nameof(DeviceTemperature),
            category: GetCategory(nameof(DeviceTemperature)),
            description: "Device temperature in degrees Celsius (C)",
            value: 20,
            min: -30,
            max: 50,
            unit: "C",
            displayPrecision: 2,
            isWritable: false,
            visibility: GcVisibility.Expert,
            selectingParameters: [nameof(DeviceTemperatureSelector)]);

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
                _timeStamp ??= new GcInteger(
                    name: nameof(TimeStamp),
                    category: GetCategory(nameof(TimeStamp)),
                    description: "Reports the current value of the device timestamp counter",
                    value: (long)Math.Round((_cameraClock.ElapsedTicks / (Stopwatch.Frequency * 10000000.0)) + _time0.Ticks, 0),
                    min: long.MinValue,
                    max: long.MaxValue,
                    increment: 1,
                    incrementMode: EIncMode.fixedIncrement,
                    unit: "ticks",
                    isWritable: false,
                    visibility: GcVisibility.Expert,
                    selectingParameters: [nameof(TimestampReset)]);

                // Putting this logic in OnParameterChanged prevents it from being updated during an GcDeviceParameterCollection.Update() run...need special case for parameters updating constantly?
                _timeStamp.Value = (long)Math.Round(_cameraClock.ElapsedTicks / (double)Stopwatch.Frequency * 10000000.0 + _time0.Ticks, 0);
                return _timeStamp;
            }
        }

        /// <summary>
        /// Provides a command to reset the timestamp in the device.
        /// </summary>
        [Category("DeviceControl")]
        public GcCommand TimestampReset => _timeStampReset ??= new GcCommand(
            name: nameof(TimestampReset),
            category: GetCategory(nameof(TimestampReset)),
            description: "Resets the current value of the device timestamp counter",
            method: () =>
            {
                _cameraClock.Restart();
                _time0 = DateTime.Now;
                TimeStamp.Value = (long)Math.Round((_cameraClock.ElapsedTicks / (Stopwatch.Frequency * 10000000.0)) + _time0.Ticks, 0);
            },
            visibility: GcVisibility.Expert,
            isSelector: true,
            selectedParameters: [nameof(TimeStamp)]);

        #endregion
    }
}