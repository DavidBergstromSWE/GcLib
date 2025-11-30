using System;
using System.ComponentModel;
using PCO.SDK.NET;

namespace GcLib;

public partial class PcoCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Private fields

        private GcString _deviceVendorName;
        private GcString _deviceModelName;
        private GcString _deviceSerialNumber;
        private GcString _deviceUserID;
        private GcEnumeration _deviceTLType;
        private GcEnumeration _deviceTemperatureSelector;
        private GcFloat _deviceTemperature;
        private GcInteger _timeStamp;

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
        public GcString DeviceUserID
        {
            get => _deviceUserID;
            set => _deviceUserID.Value = value;
        }

        /// <summary>
        /// Transport layer type.
        /// </summary>
        [Category("DeviceControl")]
        public GcEnumeration DeviceTLType => _deviceTLType;

        /// <summary>
        /// Selects which device temperature to measure in <see cref="DeviceTemperature"/>.
        /// </summary>
        [Category("DeviceControl")]
        public GcEnumeration DeviceTemperatureSelector
        {
            get => _deviceTemperatureSelector;
            set => _deviceTemperatureSelector.IntValue = value;
        }

        /// <summary>
        /// Temperature as given by device sensor selected by <see cref="DeviceTemperatureSelector"/>.
        /// </summary>
        [Category("DeviceControl")]
        public GcFloat DeviceTemperature => _deviceTemperature;

        /// <summary>
        /// Current timestamp in device.
        /// </summary>
        [Category("DeviceControl")]
        public GcInteger TimeStamp
        {
            get
            {
                // ToDo: add method to read timestamp data from PCO_Recording structure.
                _timeStamp.Value = DateTime.Now.Ticks * 10;
                return _timeStamp;
            }
        }

        /// <summary>
        /// Provides a command to reset the timestamp in the device.
        /// </summary>
        [Category("DeviceControl")]
        public GcCommand TimestampReset
        {
            get
            {
                return new(name: "TimestampReset",
                           category: "DeviceControl",
                           description: "Resets the current value of the device timestamp counter",
                           method: () =>
                                {
                                    DateTime time0 = DateTime.Now;
                                    while (time0.Millisecond != 0) { time0 = DateTime.Now; } // waits until full second 
                                    try
                                    {
                                        LibWrapper.SetDateTime(_cameraHandle, time0);
                                    }
                                    catch (Exception)
                                    {
                                        _timeStamp.Value = time0.Ticks * 10;
                                    }
                                },
                           visibility: GcVisibility.Expert,
                           isSelector: true,
                           selectedParameters: [nameof(TimeStamp)]);
            }
        }

        /// <summary>
        /// Provides a command to reset device to initial values.
        /// </summary>
        [Category("DeviceControl")]
        public GcCommand DeviceReset => _deviceReset;

        #endregion
    }
}