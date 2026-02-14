using System;
using System.ComponentModel;
using System.Diagnostics;
using GcLib.Utilities.Imaging;

namespace GcLib;

public sealed partial class VirtualCam
{
    private sealed partial class GenApi
    {
        /// <summary>
        /// Retrieves category of specified property.
        /// </summary>
        /// <param name="propertyName">Name of property.</param>
        /// <returns>Category name.</returns>
        private string GetCategory(string propertyName)
        {
            return ((CategoryAttribute)TypeDescriptor.GetProperties(this)[propertyName].Attributes[typeof(CategoryAttribute)]).Category;
        }

        /// <summary>
        /// Initializes property backing fields to default values.
        /// </summary>
        private void InitializeParameters()
        {
            _deviceVendorName = new GcString(
                name: nameof(DeviceVendorName),
                category: GetCategory(nameof(DeviceVendorName)),
                value: _virtualCam.DeviceInfo.VendorName,
                description: "Name of the manufacturer of the device",
                maxLength: 20,
                isWritable: false);

            _deviceModelName = new GcString(
                name: nameof(DeviceModelName),
                category: GetCategory(nameof(DeviceModelName)),
                value: _virtualCam.DeviceInfo.ModelName,
                description: "Model of the device",
                maxLength: 20,
                isWritable: false);

            _deviceSerialNumber = new GcString(
                name: nameof(DeviceSerialNumber),
                category: GetCategory(nameof(DeviceSerialNumber)),
                value: _virtualCam.DeviceInfo.SerialNumber,
                description: "Device's serial number",
                maxLength: 20,
                isWritable: false,
                visibility: GcVisibility.Expert);

            _deviceUserID = new GcString(
                name: nameof(DeviceUserID),
                category: GetCategory(nameof(DeviceUserID)),
                value: "CameraSimulator",
                description: "User-programmable device identifier",
                maxLength: 20);

            _deviceTLType = new GcEnumeration(
                name: nameof(DeviceTLType),
                category: GetCategory(nameof(DeviceTLType)),
                description: "Transport Layer type of the device",
                enumEntry: GcLib.DeviceTLType.Custom,
                enumArray: [GcLib.DeviceTLType.USB3Vision, GcLib.DeviceTLType.Custom],
                isWritable: false);

            _deviceTemperatureSelector = new GcEnumeration(
                name: nameof(DeviceTemperatureSelector),
                category: GetCategory(nameof(DeviceTemperatureSelector)),
                description: "Selects the location within the device, where the temperature will be measured",
                enumEntry: GcLib.DeviceTemperatureSelector.DeviceSpecific,
                enumType: typeof(DeviceTemperatureSelector),
                visibility: GcVisibility.Expert,
                isSelector: true,
                selectedParameters: [nameof(DeviceTemperature)]);

            _deviceTemperature = new GcFloat(
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

            _timeStamp = new GcInteger(
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

            _timeStampReset = new GcCommand(
                name: nameof(TimestampReset),
                category: GetCategory(nameof(TimestampReset)),
                description: "Resets the current value of the device timestamp counter",
                method: () =>
                {
                    _cameraClock.Restart();
                    _time0 = DateTime.Now;
                    _timeStamp.Value = (long)Math.Round((_cameraClock.ElapsedTicks / (Stopwatch.Frequency * 10000000.0)) + _time0.Ticks, 0); // update timestamp with new value
                },
                visibility: GcVisibility.Expert,
                isSelector: true,
                selectedParameters: [nameof(TimeStamp)]);

            _sensorWidth = new GcInteger(
                name: nameof(SensorWidth),
                category: GetCategory(nameof(SensorWidth)),
                description: "Effective width of the sensor in pixels",
                value: 640,
                min: 640,
                max: 640,
                increment: 0,
                incrementMode: EIncMode.noIncrement,
                isWritable: false,
                visibility: GcVisibility.Expert);

            _sensorHeight = new GcInteger(
                name: nameof(SensorHeight),
                category: GetCategory(nameof(SensorHeight)),
                description: "Effective height of the sensor in pixels",
                value: 480,
                min: 480,
                max: 480,
                increment: 0,
                incrementMode: EIncMode.noIncrement,
                isWritable: false,
                visibility: GcVisibility.Expert);

            _widthMax = new GcInteger(
                name: nameof(WidthMax),
                category: GetCategory(nameof(WidthMax)),
                description: "Maximum width of the image (in pixels)",
                value: _sensorWidth,
                min: 80,
                max: _sensorWidth,
                increment: 80,
                incrementMode: EIncMode.fixedIncrement,
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(BinningHorizontal)]);

            _heightMax = new GcInteger(
                name: nameof(HeightMax),
                category: GetCategory(nameof(HeightMax)),
                description: "Maximum height of the image (in pixels)",
                value: _sensorHeight,
                min: 60,
                max: _sensorHeight,
                increment: 60,
                incrementMode: EIncMode.fixedIncrement,
                unit: "",
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(BinningVertical)]);

            _width = new GcInteger(
                name: nameof(Width),
                category: GetCategory(nameof(Width)),
                description: "Width of the image provided by the device (in pixels)",
                value: 320,
                min: _widthMax.Min,
                max: _widthMax.Value,
                increment: _widthMax.Increment,
                incrementMode: EIncMode.fixedIncrement,
                unit: "",
                isSelector: true,
                selectedParameters: [nameof(OffsetX)]);

            _height = new GcInteger(
                name: nameof(Height),
                category: GetCategory(nameof(Height)),
                description: "Height of the image provided by the device (in pixels)",
                value: 240,
                min: _heightMax.Min,
                max: _heightMax.Value,
                increment: _heightMax.Increment,
                incrementMode: EIncMode.fixedIncrement,
                unit: "",
                isSelector: true,
                selectedParameters: [nameof(OffsetY)]);

            _offsetX = new GcInteger(
                name: nameof(OffsetX),
                category: GetCategory(nameof(OffsetX)),
                description: "Horizontal offset from the origin to the region of interest (in pixels)",
                value: 0,
                min: 0,
                max: _widthMax - _width,
                increment: _width.Increment,
                incrementMode: EIncMode.fixedIncrement,
                unit: "",
                selectingParameters: [nameof(Width)]);

            _offsetY = new GcInteger(
                name: nameof(OffsetY),
                category: GetCategory(nameof(OffsetY)),
                description: "Vertical offset from the origin to the region of interest (in pixels)",
                value: 0,
                min: 0,
                max: _heightMax - _height,
                increment: _height.Increment,
                incrementMode: EIncMode.fixedIncrement,
                unit: "",
                selectingParameters: [nameof(Height)]);

            _binningHorizontal = new GcInteger(
                name: nameof(BinningHorizontal),
                category: GetCategory(nameof(BinningHorizontal)),
                description: "Number of horizontal photo-sensitive cells to combine together",
                value: 1,
                min: 1,
                max: 4,
                increment: 0,
                incrementMode: EIncMode.listIncrement,
                unit: "",
                listOfValidValue: [1, 2, 4],
                visibility: GcVisibility.Expert,
                isSelector: true);

            _binningVertical = new GcInteger(
                name: nameof(BinningVertical),
                category: GetCategory(nameof(BinningVertical)),
                description: "Number of vertical photo-sensitive cells to combine together",
                value: 1,
                min: 1,
                max: 4,
                increment: 0,
                incrementMode: EIncMode.listIncrement,
                unit: "",
                listOfValidValue: [1, 2, 4],
                visibility: GcVisibility.Expert,
                isSelector: true);

            _reverseX = new GcBoolean(nameof(ReverseX)); // not implemented

            _reverseY = new GcBoolean(nameof(ReverseX)); // not implemented

            _pixelFormat = new GcEnumeration(
                name: nameof(PixelFormat),
                category: GetCategory(nameof(PixelFormat)),
                description: "Format of the pixels provided by the device",
                enumEntry: GcLib.PixelFormat.Mono8,
                enumArray: [ GcLib.PixelFormat.Mono8, GcLib.PixelFormat.Mono10, GcLib.PixelFormat.Mono12, GcLib.PixelFormat.Mono14, GcLib.PixelFormat.Mono16, 
                             GcLib.PixelFormat.RGB8, GcLib.PixelFormat.RGB10, GcLib.PixelFormat.RGB12, GcLib.PixelFormat.RGB14, GcLib.PixelFormat.RGB16, 
                             GcLib.PixelFormat.BGR8, GcLib.PixelFormat.BGR10, GcLib.PixelFormat.BGR12, GcLib.PixelFormat.BGR14, GcLib.PixelFormat.BGR16],
                isWritable: true,
                isSelector: true,
                selectedParameters: [nameof(PixelSize)]);

            _pixelSize = new GcEnumeration(
                name: nameof(PixelSize),
                category: GetCategory(nameof(PixelSize)),
                description: "Total size in bits of a pixel of the image",
                enumEntry: GenICamHelper.GetPixelSize((PixelFormat)PixelFormat.IntValue),
                enumArray: [GcLib.PixelSize.Bpp8, GcLib.PixelSize.Bpp10, GcLib.PixelSize.Bpp12, GcLib.PixelSize.Bpp14, GcLib.PixelSize.Bpp16],
                isWritable: false,
                visibility: GcVisibility.Expert,
                selectingParameters: [nameof(PixelFormat)]);

            _testPattern = new GcEnumeration(
                name: nameof(TestPattern),
                category: GetCategory(nameof(TestPattern)),
                description: "Selects the type of test pattern that is generated by the device as image source",
                enumEntry: GcLib.TestPattern.GrayVerticalRampMoving,
                enumArray: [GcLib.TestPattern.Black, GcLib.TestPattern.White, GcLib.TestPattern.GrayVerticalRamp, GcLib.TestPattern.GrayVerticalRampMoving, GcLib.TestPattern.GrayHorizontalRamp, GcLib.TestPattern.GrayHorizontalRampMoving, GcLib.TestPattern.VerticalLineMoving, GcLib.TestPattern.HorizontalLineMoving, GcLib.TestPattern.FrameCounter, GcLib.TestPattern.ImageDirectory, GcLib.TestPattern.WhiteNoise]);

            _imageDirectory = new GcString(
                name: nameof(ImageDirectory),
                category: GetCategory(nameof(ImageDirectory)),
                value: "",
                maxLength: 260,
                description: $"Specifies folder to acquire images from when {nameof(TestPattern)} is {nameof(GcLib.TestPattern.ImageDirectory)}");

            _acquisitionMode = new GcEnumeration(
                name: nameof(AcquisitionMode),
                category: GetCategory(nameof(AcquisitionMode)),
                description: "Sets the acquisition mode of the device",
                enumEntry: GcLib.AcquisitionMode.Continuous,
                enumType: typeof(AcquisitionMode));

            _acquisitionFrameCount = new GcInteger(
                name: nameof(AcquisitionFrameCount),
                category: GetCategory(nameof(AcquisitionFrameCount)),
                description: $"Number of frames to acquire when {nameof(AcquisitionMode)} is set to {nameof(GcLib.AcquisitionMode.MultiFrame)}",
                value: 10,
                min: 1,
                max: 30,
                increment: 1,
                incrementMode: EIncMode.fixedIncrement,
                unit: "");

            _acquisitionFrameRate = new GcFloat(
                name: nameof(AcquisitionFrameRate),
                category: GetCategory(nameof(AcquisitionFrameRate)),
                description: "Controls the acquisition rate (in Hertz) at which the frames are captured",
                value: 30,
                min: 1,
                max: 60,
                increment: 0.1,
                displayPrecision: 1,
                unit: "Hz");

            _testString = new GcString(
                name: nameof(TestString),
                category: GetCategory(nameof(TestString)),
                maxLength: 20,
                value: "HelloWorld",
                visibility: GcVisibility.Guru);

            _testInteger = new GcInteger(
                name: nameof(TestInteger),
                category: GetCategory(nameof(TestInteger)),
                value: 42,
                min: 0, 
                max: 100,
                increment: 1,
                incrementMode: EIncMode.fixedIncrement,
                visibility: GcVisibility.Guru);

            _testEnumeration = new GcEnumeration(
                name: nameof(TestEnumeration),
                category: GetCategory(nameof(TestEnumeration)),
                DayOfWeek.Friday,
                typeof(DayOfWeek),
                visibility: GcVisibility.Guru);

            _testBoolean = new GcBoolean(
                name: nameof(TestBoolean),
                category: "Test",
                description: "Parameter added to test boolean type camera properties",
                value: false,
                isWritable: true,
                visibility: GcVisibility.Guru);

            _testFloat = new GcFloat(
                name: nameof(TestFloat),
                category: GetCategory(nameof(TestFloat)),
                description: "Parameter added to test float type camera properties",
                value: 3.1,
                min: 0.0,
                max: 9.9,
                unit: "m",
                increment: 0.1,
                displayPrecision: 1,
                isWritable: true,
                visibility: GcVisibility.Guru);

            _testCommand = new GcCommand(
                name: nameof(TestCommand),
                category: GetCategory(nameof(TestCommand)),
                description: "Parameter added to test command type camera properties.",
                method: () => { },
                visibility: GcVisibility.Guru);

            _acquisitionFailure = new GcBoolean(
                name: nameof(AcquisitionFailure),
                category: GetCategory(nameof(AcquisitionFailure)),
                description: $"Simulates a hardware failure during acquisition. The type of failure is defined in {nameof(AcquisitionFailureEvent)} and the time to failure is given by {nameof(AcquisitionTimeToFailure)}.",
                value: false,
                visibility: GcVisibility.Guru);

            _acquisitionFailureEvent = new GcEnumeration(
                name: nameof(AcquisitionFailureEvent),
                category: GetCategory(nameof(AcquisitionFailureEvent)),
                description: "Event defining the type of failure to simulate.",
                enumEntry: AcquisitionFailureEventType.ConnectionLost,
                enumType: typeof(AcquisitionFailureEventType),
                visibility: GcVisibility.Guru);

            _acquisitionTimeToFailure = new GcInteger(
                name: nameof(AcquisitionTimeToFailure),
                category: GetCategory(nameof(AcquisitionTimeToFailure)),
                description: $"Time to hardware failure in milliseconds after a new acquisition has been started. To simulate hardware failure {nameof(AcquisitionFailure)} need to be set to true. The type of failure is defined by {nameof(AcquisitionFailureEvent)}.",
                value: 1000,
                min: 0,
                max: 5000,
                increment: 1,
                incrementMode: EIncMode.fixedIncrement,
                visibility: GcVisibility.Guru);
        }
    }
}