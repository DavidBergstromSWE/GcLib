﻿using System;
using System.ComponentModel;

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
        private GcEnumeration _binningHorizontalMode;
        private GcInteger _binningHorizontal;
        private GcEnumeration _binningVerticalMode;
        private GcInteger _binningVertical;
        private GcBoolean _reverseX;
        private GcBoolean _reverseY;
        private GcEnumeration _pixelFormat;
        private GcEnumeration _pixelSize;
        private GcEnumeration _testPattern;
        private GcString _imageDirectory;

        #endregion

        // Provides properties for controlling image format.

        #region Image Format Control

        /// <summary>
        /// Effective width of the sensor in pixels.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger SensorWidth => _sensorWidth;

        /// <summary>
        /// Effective height of the sensor in pixels.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger SensorHeight => _sensorHeight;

        /// <summary>
        /// Maximum width of the image (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger WidthMax => _widthMax;

        /// <summary>
        /// Maximum height of the image (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger HeightMax => _heightMax;

        /// <summary>
        /// Width of the image provided by the device (in pixels). 
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(320)]
        public GcInteger Width => _width;

        /// <summary>
        /// Height of the image provided by the device (in pixels). 
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(240)]
        public GcInteger Height => _height;

        /// <summary>
        /// Horizontal offset from the origin to the region of interest (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(0)]
        public GcInteger OffsetX => _offsetX;

        /// <summary>
        /// Vertical offset from the origin to the region of interest (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(0)]
        public GcInteger OffsetY => _offsetY;

        /// <summary>
        /// Mode to use to combine horizontal pixels when <see cref="BinningHorizontal"/> is used.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.BinningHorizontalMode.Average)]
        public GcEnumeration BinningHorizontalMode => _binningHorizontalMode;

        /// <summary>
        /// Number of horizontal pixels to combine together.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(1)]
        public GcInteger BinningHorizontal => _binningHorizontal;

        /// <summary>
        /// Mode to use to combine vertical pixels when <see cref="BinningVertical"/> is used.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.BinningVerticalMode.Average)]
        public GcEnumeration BinningVerticalMode => _binningVerticalMode;

        /// <summary>
        /// Number of vertical pixels to combine together.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(1)]
        public GcInteger BinningVertical => _binningVertical;

        /// <summary>
        /// Flips the image sent by the device horizontally.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcBoolean ReverseX => _reverseX;

        /// <summary>
        /// Flips the image sent by the device vertically.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcBoolean ReverseY => _reverseY;

        /// <summary>
        /// Format of the pixels provided by the device.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.PixelFormat.Mono8)]
        public GcEnumeration PixelFormat => _pixelFormat;

        /// <summary>
        /// Total size in bits of a pixel of the image.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.PixelSize.Bpp8)]
        public GcEnumeration PixelSize => _pixelSize;

        /// <summary>
        /// Selects the type of test pattern that is generated by the device as image source.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue(GcLib.TestPattern.GrayVerticalRampMoving)]
        public GcEnumeration TestPattern => _testPattern;

        /// <summary>
        /// Selects the image directory to use as image source when <see cref="TestPattern"/> is set to <see cref="TestPattern.ImageDirectory"/>.
        /// </summary>
        [Category("ImageFormatControl")]
        [DefaultValue("")]
        public GcString ImageDirectory => _imageDirectory;

        #endregion
    }
}