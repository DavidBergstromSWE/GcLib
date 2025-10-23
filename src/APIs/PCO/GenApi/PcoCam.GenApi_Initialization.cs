using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using PCO.SDK;

namespace GcLib;

public partial class PcoCam
{
    private sealed partial class GenApi
    {
        private string GetCategory(string propertyName)
        {
            return ((CategoryAttribute)TypeDescriptor.GetProperties(this)[propertyName].Attributes[typeof(CategoryAttribute)]).Category;
        }

        /// <summary>
        /// Initialize backing fields of device properties.
        /// </summary>
        private void InitializeParameters()
        {
            _deviceVendorName = new GcString(
                name: nameof(DeviceVendorName),
                category: GetCategory(nameof(DeviceVendorName)),
                value: "PCO AG",
                description: "Name of the manufacturer of the device",
                maxLength: 6,
                isWritable: false);

            _deviceModelName = new GcString(
                name: nameof(DeviceModelName),
                category: GetCategory(nameof(DeviceModelName)),
                value: LibWrapper.GetCameraName(_cameraHandle),
                description: "Model of the device",
                maxLength: 40,
                isWritable: false);

            _deviceSerialNumber = new GcString(
                name: nameof(DeviceSerialNumber),
                category: GetCategory(nameof(DeviceSerialNumber)),
                value: LibWrapper.GetCameraSerialNumber(_cameraHandle),
                description: "Device's serial number",
                maxLength: 40,
                isWritable: false,
                visibility: GcVisibility.Expert);

            _deviceUserID = new GcString(
                name: nameof(DeviceUserID),
                category: GetCategory(nameof(DeviceUserID)),
                value: string.Empty,
                description: "User-programmable device identifier",
                maxLength: 40);

            _deviceTLType = new GcEnumeration(
                name: nameof(DeviceTLType),
                category: GetCategory(nameof(DeviceTLType)),
                description: "Transport Layer type of the device",
                enumEntry: GcLib.DeviceTLType.USB, // need method to convert PCO interface type codes (2.3.2.3) to DeviceTLType here             
                enumType: typeof(DeviceTLType),
                isWritable: false);

            _deviceTemperatureSelector = new GcEnumeration(
                name: nameof(DeviceTemperatureSelector),
                category: GetCategory(nameof(DeviceTemperatureSelector)),
                description: "Selects the location within the device, where the temperature will be measured",
                enumEntry: GcLib.DeviceTemperatureSelector.Sensor,
                enumType: typeof(DeviceTemperatureSelector),
                visibility: GcVisibility.Expert,
                isSelector: true,
                selectedParameters: [nameof(DeviceTemperature)]);

            _deviceTemperature = new GcFloat(
                name: nameof(DeviceTemperature),
                category: GetCategory(nameof(DeviceTemperature)),
                description: "Device temperature in degrees Celsius (C)",
                value: LibWrapper.GetCameraTemperature(_cameraHandle, (TemperatureSelector)_deviceTemperatureSelector.IntValue),
                min: short.MinValue,
                max: short.MaxValue,
                unit: "C",
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(DeviceTemperatureSelector)]);

            _timeStamp = new GcInteger(
                name: nameof(TimeStamp),
                category: GetCategory(nameof(TimeStamp)),
                description: "Reports the current value of the device timestamp counter",
                value: DateTime.Now.Ticks * 10, // should be readable from PCO_Recording struct...
                min: long.MinValue,
                max: long.MaxValue,
                increment: 1, incrementMode:
                EIncMode.fixedIncrement,
                unit: "ticks",
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(TimestampReset)]);

            LibWrapper.GetSensorFormat(_cameraHandle, out ushort sensorWidth, out ushort sensorHeight, out ushort minimumWidth, out ushort minimumHeight); // maximum width and height of camera

            _sensorWidth = new GcInteger(
                name: nameof(SensorWidth),
                category: GetCategory(nameof(SensorWidth)),
                description: "Effective width of the sensor in pixels",
                value: sensorWidth,
                min: sensorWidth,
                max: sensorWidth,
                increment: 0,
                incrementMode: EIncMode.noIncrement,
                isWritable: false,
                visibility: GcVisibility.Expert);

            _sensorHeight = new GcInteger(
                name: nameof(SensorHeight),
                category: GetCategory(nameof(SensorHeight)),
                description: "Effective height of the sensor in pixels",
                value: sensorHeight,
                min: sensorHeight,
                max: sensorHeight,
                increment: 0,
                incrementMode: EIncMode.noIncrement,
                isWritable: false,
                visibility: GcVisibility.Expert);

            LibWrapper.GetBinningLimits(_cameraHandle, out ushort maximumHorizontalBinning, out ushort maximumVerticalBinning, out ushort horizontalBinningStep, out ushort verticalBinningStep);

            _binningHorizontal = new GcInteger(
                name: nameof(BinningHorizontal),
                category: GetCategory(nameof(BinningHorizontal)),
                description: "Number of horizontal photo-sensitive cells to combine together",
                value: LibWrapper.GetBinning(_cameraHandle, BinningOrientation.Horizontal),
                min: 1,
                max: maximumHorizontalBinning,
                increment: horizontalBinningStep,
                incrementMode: EIncMode.listIncrement,
                listOfValidValue: LibWrapper.GetValidBinningModes(_cameraHandle, BinningOrientation.Horizontal),
                visibility: GcVisibility.Expert,
                isSelector: true,
                selectedParameters: [nameof(WidthMax), nameof(Width), nameof(OffsetX)]);

            _binningVertical = new GcInteger(
                name: nameof(BinningVertical),
                category: GetCategory(nameof(BinningVertical)),
                description: "Number of vertical photo-sensitive cells to combine together",
                value: LibWrapper.GetBinning(_cameraHandle, BinningOrientation.Vertical),
                min: 1,
                max: maximumVerticalBinning,
                increment: verticalBinningStep, // 
                incrementMode: EIncMode.listIncrement,
                listOfValidValue: LibWrapper.GetValidBinningModes(_cameraHandle, BinningOrientation.Vertical),
                visibility: GcVisibility.Expert,
                isSelector: true,
                selectedParameters: [nameof(HeightMax), nameof(Height), nameof(OffsetY)]);

            LibWrapper.GetROI(_cameraHandle, out ushort width, out ushort height, out ushort offsetX, out ushort offsetY);
            LibWrapper.GetROISteps(_cameraHandle, out ushort horizontalStep, out ushort verticalStep);

            _widthMax = new GcInteger(
                name: nameof(WidthMax),
                category: GetCategory(nameof(WidthMax)),
                description: "Maximum width of the image (in pixels)",
                value: width,
                min: minimumWidth,
                max: sensorWidth,
                increment: horizontalStep,
                incrementMode: EIncMode.fixedIncrement,
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(BinningHorizontal)]);

            _heightMax = new GcInteger(
                name: nameof(HeightMax),
                category: GetCategory(nameof(HeightMax)),
                description: "Maximum height of the image (in pixels)",
                value: height,
                min: minimumHeight,
                max: sensorHeight,
                increment: verticalStep,
                incrementMode: EIncMode.fixedIncrement,
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(BinningVertical)]);

            _width = new GcInteger(
                name: nameof(Width),
                category: GetCategory(nameof(Width)),
                description: "Width of the image provided by the device (in pixels)",
                value: width,
                min: minimumWidth,
                max: sensorWidth,
                increment: horizontalStep,
                incrementMode: EIncMode.fixedIncrement,
                isSelector: true,
                selectedParameters: [nameof(OffsetX), nameof(AcquisitionFrameRate)],
                selectingParameters: [nameof(BinningHorizontal)]);

            _height = new GcInteger(
                name: nameof(Height),
                category: GetCategory(nameof(Height)),
                description: "Height of the image provided by the device (in pixels)",
                value: height,
                min: minimumHeight,
                max: sensorHeight,
                increment: verticalStep,
                incrementMode: EIncMode.fixedIncrement,
                isSelector: true,
                selectedParameters: [nameof(OffsetY), nameof(AcquisitionFrameRate)],
                selectingParameters: [nameof(BinningVertical)]);

            _offsetX = new GcInteger(
                name: nameof(OffsetX),
                category: GetCategory(nameof(OffsetX)),
                description: "Horizontal offset from the origin to the region of interest (in pixels)",
                value: offsetX,
                min: 0, // 0 = no offset
                max: _widthMax - _width,     // ?
                increment: horizontalStep,
                incrementMode: EIncMode.fixedIncrement,
                selectingParameters: [nameof(Width)]);

            _offsetY = new GcInteger(
                name: nameof(OffsetY),
                category: GetCategory(nameof(OffsetY)),
                description: "Vertical offset from the origin to the region of interest (in pixels)",
                value: offsetY,
                min: 0, // 0 = no offset
                max: _heightMax - _height,    // max?
                increment: verticalStep,
                incrementMode: EIncMode.fixedIncrement,
                selectingParameters: [nameof(Height)]);

            _pixelFormat = new GcEnumeration(
                name: nameof(PixelFormat),
                category: GetCategory(nameof(PixelFormat)),
                description: "Format of the pixels provided by the device",
                enumEntry: GcLib.PixelFormat.Mono16, // needs a method!
                enumArray: [GcLib.PixelFormat.Mono16], // needs a method!
                isSelector: true,
                selectedParameters: [nameof(PixelSize)]);

            _pixelSize = new GcEnumeration(
                name: nameof(PixelSize),
                category: GetCategory(nameof(PixelSize)),
                description: "Total size in bits of a pixel of the image",
                enumEntry: GcLib.PixelSize.Bpp16, // needs a method!
                enumArray: [GcLib.PixelSize.Bpp16], // needs a method!
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(PixelFormat)]);

            _deviceReset = new GcCommand(
                name: nameof(DeviceReset),
                category: GetCategory(nameof(DeviceReset)),
                method: () =>
                {
                    LibWrapper.ResetSettingsToDefault(_cameraHandle);
                    InitializeParameters(); // reset to initial parameter values
                },
                visibility: GcVisibility.Guru,
                isSelector: true,
                selectedParameters: [.. GetParameterNames()]);

            _acquisitionMode = new GcEnumeration(
                name: nameof(AcquisitionMode),
                category: GetCategory(nameof(AcquisitionMode)),
                description: "Sets the acquisition mode of the device",
                enumEntry: GcLib.AcquisitionMode.Continuous,
                enumType: typeof(AcquisitionMode));

            _acquisitionFrameCount = new GcInteger(
                name: nameof(AcquisitionFrameCount),
                category: GetCategory(nameof(AcquisitionFrameCount)),
                description: "Number of frames to acquire in MultiFrame Acquisition mode",
                value: 10,
                min: 1,
                max: 30,
                increment: 1,
                incrementMode: EIncMode.fixedIncrement);

            _acquisitionFrameRate = new GcFloat(
                name: nameof(AcquisitionFrameRate),
                category: GetCategory(nameof(AcquisitionFrameRate)),
                description: "Controls the acquisition rate (in Hertz) at which the frames are captured",
                value: LibWrapper.GetFrameRate(_cameraHandle),
                min: 0, // needs a method! calculate from exposure time and delay limits?
                max: 100, // needs a method! calculate from exposure time and delay limits?
                increment: 0.1,
                unit: "Hz",
                displayPrecision: 1,
                selectingParameters: [nameof(Width), nameof(Height)],
                selectedParameters: [nameof(ExposureTime)]);

            LibWrapper.GetExposureTimeLimits(_cameraHandle, out uint minExposure, out uint maxExposure, out double stepExposure);

            _exposureTime = new GcFloat( // should be GcInteger instead? increment: _cameraDescription.dwMinExposStepDESC/1000 not work?
                name: nameof(ExposureTime),
                category: GetCategory(nameof(ExposureTime)),
                description: "Controls the duration where the photosensitive cells are exposed to light, in the unit of microseconds",
                value: LibWrapper.GetExposureTime(_cameraHandle),
                min: minExposure,
                max: maxExposure,
                unit: "us",
                increment: stepExposure,
                //displayPrecision: (long)Math.Round(Math.Log10(1.0 / Convert.ToDouble(Convert.ToDecimal(_cameraDescription.dwMinExposStepDESC) / 1000))), // will not work!
                selectingParameters: [nameof(AcquisitionFrameRate)]);

            _triggerMode = new GcEnumeration(
                name: nameof(TriggerMode),
                category: GetCategory(nameof(TriggerMode)),
                description: "Acquisition trigger mode", 
                enumEntry: LibWrapper.GetTriggerMode(_cameraHandle),
                enumType: typeof(TriggerMode));

            _noiseFilterMode = new GcEnumeration(
                name: nameof(NoiseFilterMode),
                category: GetCategory(nameof(NoiseFilterMode)),
                description: "Noise correction mode (can be set to off, noise filter only or noise filter plus hot pixel correction).",
                enumEntry: LibWrapper.GetNoiseFilterMode(_cameraHandle),
                enumType: typeof(NoiseFilterMode)
                );

            //SetBufferCapacity(2);
            _inputBufferCount = new GcInteger(
                name: nameof(InputBufferCount),
                category: GetCategory(nameof(InputBufferCount)),
                description: "Number of buffers in the input buffer pool.",
                value: 2,
                min: 1,
                max: 16,
                increment: 1,
                incrementMode: EIncMode.fixedIncrement,
                visibility: GcVisibility.Guru
                );
        }

        /// <summary>
        /// Returns a complete list of strings of all parameter names.
        /// </summary>
        /// <returns>List of parameter names.</returns>
        private List<string> GetParameterNames()
        {
            var parameters = new List<string>();

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
    }
}