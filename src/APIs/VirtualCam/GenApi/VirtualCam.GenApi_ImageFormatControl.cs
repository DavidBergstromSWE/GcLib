using System;
using System.ComponentModel;
using GcLib.Utilities.Imaging;

namespace GcLib;

public sealed partial class VirtualCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Backing fields

        private GcInteger _sensorWidth;
        private GcInteger _sensorHeight;
        private GcInteger _widthMax;
        private GcInteger _heightMax;
        private GcInteger _width;
        private GcInteger _height;
        private GcInteger _offsetX;
        private GcInteger _offsetY;
        private GcInteger _binningHorizontal;
        private GcInteger _binningVertical;
        private GcBoolean _reverseX;
        private GcBoolean _reverseY;
        private GcEnumeration _pixelFormat;
        private GcEnumeration _pixelSize;
        private GcEnumeration _testPattern;
        private GcString _imageDirectory;
        private GcInteger _payloadSize;

        #endregion

        // Provides properties for controlling image format.

        #region Image Format Control

        /// <summary>
        /// Effective width of the sensor in pixels.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger SensorWidth => _sensorWidth ??= new GcInteger(
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

        /// <summary>
        /// Effective height of the sensor in pixels.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger SensorHeight => _sensorHeight ??= new GcInteger(
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

        /// <summary>
        /// Maximum width of the image (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger WidthMax => _widthMax ??= new GcInteger(
            name: nameof(WidthMax),
            category: GetCategory(nameof(WidthMax)),
            description: "Maximum width of the image (in pixels)",
            value: SensorWidth.Value,
            min: 80,
            max: SensorWidth.Value,
            increment: 80,
            incrementMode: EIncMode.fixedIncrement,
            isWritable: false,
            visibility: GcVisibility.Expert,
            selectingParameters: [nameof(BinningHorizontal)]);

        /// <summary>
        /// Maximum height of the image (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger HeightMax => _heightMax ??= new GcInteger(
            name: nameof(HeightMax),
            category: GetCategory(nameof(HeightMax)),
            description: "Maximum height of the image (in pixels)",
            value: SensorHeight.Value,
            min: 60,
            max: SensorHeight.Value,
            increment: 60,
            incrementMode: EIncMode.fixedIncrement,
            unit: "",
            isWritable: false,
            visibility: GcVisibility.Expert,
            selectingParameters: [nameof(BinningVertical)]);

        /// <summary>
        /// Width of the image provided by the device (in pixels). 
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(320)]
        public GcInteger Width => _width ??= new GcInteger(
            name: nameof(Width),
            category: GetCategory(nameof(Width)),
            description: "Width of the image provided by the device (in pixels)",
            value: 640,
            min: WidthMax.Min,
            max: WidthMax.Value,
            increment: WidthMax.Increment,
            incrementMode: EIncMode.fixedIncrement,
            unit: "",
            isSelector: true,
            selectedParameters: [nameof(OffsetX)]);

        /// <summary>
        /// Height of the image provided by the device (in pixels). 
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(240)]
        public GcInteger Height => _height ??= new GcInteger(
            name: nameof(Height),
            category: GetCategory(nameof(Height)),
            description: "Height of the image provided by the device (in pixels)",
            value: 480,
            min: HeightMax.Min,
            max: HeightMax.Value,
            increment: HeightMax.Increment,
            incrementMode: EIncMode.fixedIncrement,
            unit: "",
            isSelector: true,
            selectedParameters: [nameof(OffsetY)]);

        /// <summary>
        /// Horizontal offset from the origin to the region of interest (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(0)]
        public GcInteger OffsetX => _offsetX ??= new GcInteger(
            name: nameof(OffsetX),
            category: GetCategory(nameof(OffsetX)),
            description: "Horizontal offset from the origin to the region of interest (in pixels)",
            value: 0,
            min: 0,
            max: WidthMax - Width,
            increment: Width.Increment,
            incrementMode: EIncMode.fixedIncrement,
            unit: "",
            selectingParameters: [nameof(Width)]);

        /// <summary>
        /// Vertical offset from the origin to the region of interest (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(0)]
        public GcInteger OffsetY => _offsetY ??= new GcInteger(
            name: nameof(OffsetY),
            category: GetCategory(nameof(OffsetY)),
            description: "Vertical offset from the origin to the region of interest (in pixels)",
            value: 0,
            min: 0,
            max: HeightMax - Height,
            increment: Height.Increment,
            incrementMode: EIncMode.fixedIncrement,
            unit: "",
            selectingParameters: [nameof(Height)]);

        /// <summary>
        /// Number of horizontal pixels to combine together.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(1)]
        public GcInteger BinningHorizontal => _binningHorizontal ??= new GcInteger(
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

        /// <summary>
        /// Number of vertical pixels to combine together.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(1)]
        public GcInteger BinningVertical => _binningVertical ??= new GcInteger(
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

        /// <summary>
        /// Flips the image sent by the device horizontally.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(false)]
        public GcBoolean ReverseX => _reverseX ??= new GcBoolean(
            name: nameof(ReverseX),
            category: GetCategory(nameof(ReverseX)),
            description: "Flips the image sent by the device horizontally",
            value: false,
            visibility: GcVisibility.Expert);

        /// <summary>
        /// Flips the image sent by the device vertically.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(false)]
        public GcBoolean ReverseY => _reverseY ??= new GcBoolean(
            name: nameof(ReverseY),
            category: GetCategory(nameof(ReverseY)),
            description: "Flips the image sent by the device vertically",
            value: false,
            visibility: GcVisibility.Expert);

        /// <summary>
        /// Format of the pixels provided by the device.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.PixelFormat.Mono8)]
        public GcEnumeration PixelFormat => _pixelFormat ??= new GcEnumeration(
            name: nameof(PixelFormat),
            category: GetCategory(nameof(PixelFormat)),
            description: "Format of the pixels provided by the device",
            enumEntry: GcLib.PixelFormat.Mono8,
            enumArray: [GcLib.PixelFormat.Mono8, GcLib.PixelFormat.Mono10, GcLib.PixelFormat.Mono12, GcLib.PixelFormat.Mono14, GcLib.PixelFormat.Mono16,
                        GcLib.PixelFormat.RGB8, GcLib.PixelFormat.RGB10, GcLib.PixelFormat.RGB12, GcLib.PixelFormat.RGB14, GcLib.PixelFormat.RGB16,
                        GcLib.PixelFormat.BGR8, GcLib.PixelFormat.BGR10, GcLib.PixelFormat.BGR12, GcLib.PixelFormat.BGR14, GcLib.PixelFormat.BGR16],
            isWritable: true,
            isSelector: true,
            selectedParameters: [nameof(PixelSize)]);

        /// <summary>
        /// Total size in bits of a pixel of the image.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.PixelSize.Bpp8)]
        public GcEnumeration PixelSize => _pixelSize ??= new GcEnumeration(
            name: nameof(PixelSize),
            category: GetCategory(nameof(PixelSize)),
            description: "Total size in bits of a pixel of the image",
            enumEntry: GenICamHelper.GetPixelSize((PixelFormat)PixelFormat.IntValue),
            enumArray: [GcLib.PixelSize.Bpp8, GcLib.PixelSize.Bpp10, GcLib.PixelSize.Bpp12, GcLib.PixelSize.Bpp14, GcLib.PixelSize.Bpp16],
            isWritable: false,
            visibility: GcVisibility.Expert,
            selectingParameters: [nameof(PixelFormat)]);

        /// <summary>
        /// Selects the type of test pattern that is generated by the device as image source.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.TestPattern.GrayVerticalRampMoving)]
        public GcEnumeration TestPattern => _testPattern ??= new GcEnumeration(
            name: nameof(TestPattern),
            category: GetCategory(nameof(TestPattern)),
            description: "Selects the type of test pattern that is generated by the device as image source",
            enumEntry: GcLib.TestPattern.GrayVerticalRampMoving,
            enumArray: [GcLib.TestPattern.Black, GcLib.TestPattern.White, GcLib.TestPattern.GrayVerticalRamp, GcLib.TestPattern.GrayVerticalRampMoving, GcLib.TestPattern.GrayHorizontalRamp, GcLib.TestPattern.GrayHorizontalRampMoving, GcLib.TestPattern.VerticalLineMoving, GcLib.TestPattern.HorizontalLineMoving, GcLib.TestPattern.FrameCounter, GcLib.TestPattern.ImageDirectory, GcLib.TestPattern.WhiteNoise]);

        /// <summary>
        /// Selects the image directory to use as image source when <see cref="TestPattern"/> is set to <see cref="TestPattern.ImageDirectory"/>.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue("")]
        public GcString ImageDirectory => _imageDirectory ??= new GcString(
            name: nameof(ImageDirectory),
            category: GetCategory(nameof(ImageDirectory)),
            value: "",
            maxLength: 260,
            description: $"Specifies folder to acquire images from when {nameof(TestPattern)} is {nameof(GcLib.TestPattern.ImageDirectory)}");

        #endregion

        #region Transport Layer Control

        /// <summary>
        /// Gets the payload size, in bytes.
        /// </summary>
        [Category("TransportLayerControl")]
        public GcInteger PayloadSize => _payloadSize ??= new GcInteger(
            name: nameof(PayloadSize),
            category: GetCategory(nameof(PayloadSize)),
            description: "Size of the image payload in bytes",
            value: Width.Value * Height.Value * GenICamHelper.GetBitsPerPixel((PixelFormat)PixelFormat.IntValue) / 8,
            min: 0,
            max: int.MaxValue,
            increment: 1,
            incrementMode: EIncMode.fixedIncrement,
            isWritable: false,
            visibility: GcVisibility.Expert);

        #endregion
    }
}