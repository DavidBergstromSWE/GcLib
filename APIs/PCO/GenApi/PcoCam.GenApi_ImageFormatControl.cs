using System;
using System.ComponentModel;

namespace GcLib;

public partial class PcoCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Private fields

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
        private GcEnumeration _pixelFormat;
        private GcEnumeration _pixelSize;
        private GcCommand _deviceReset;
        private GcEnumeration _noiseFilterMode;

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
        public GcInteger Width
        {
            get => _width;
            set => _width.Value = value;
        }

        /// <summary>
        /// Height of the image provided by the device (in pixels). 
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger Height
        {
            get => _height;
            set => _height.Value = value;
        }

        /// <summary>
        /// Horizontal offset from the origin to the region of interest (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger OffsetX
        {
            get => _offsetX;
            set => _offsetX.Value = value;
        }

        /// <summary>
        /// Vertical offset from the origin to the region of interest (in pixels).
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger OffsetY
        {
            get => _offsetY;
            set => _offsetY.Value = value;
        }

        /// <summary>
        /// Number of horizontal pixels to combine together.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger BinningHorizontal
        {
            get => _binningHorizontal;
            set => _binningHorizontal.Value = value;
        }

        /// <summary>
        /// Number of vertical pixels to combine together.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcInteger BinningVertical
        {
            get => _binningVertical;
            set => _binningVertical.Value = value;
        }

        /// <summary>
        /// Format of the pixels provided by the device.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcEnumeration PixelFormat
        {
            get => _pixelFormat;
            set => _pixelFormat.IntValue = value;
        }

        /// <summary>
        /// Total size in bits of a pixel of the image.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcEnumeration PixelSize
        {
            get => _pixelSize;
            set => _pixelSize.IntValue = value;
        }

        /// <summary>
        /// Noise filtering mode.
        /// </summary>
        [Category("ImageFormatControl")]
        public GcEnumeration NoiseFilterMode
        {
            get => _noiseFilterMode;
            set => _noiseFilterMode.IntValue = value;
        }

        #endregion
    }
}