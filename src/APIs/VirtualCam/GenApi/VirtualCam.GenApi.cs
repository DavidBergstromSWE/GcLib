using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Timers;
using GcLib.Utilities.Imaging;

namespace GcLib;

public sealed partial class VirtualCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Fields

        /// <summary>
        /// Camera (outer class) reference.
        /// </summary>
        private readonly VirtualCam _virtualCam;

        /// <summary>
        /// Internal camera clock.
        /// </summary>
        private readonly Stopwatch _cameraClock;

        /// <summary>
        /// Timer object (event generator) for ImagePatternGenerator.
        /// </summary>
        private Timer _imagePatternGeneratorTimer;

        /// <summary>
        /// DateTime object for translating camera clock to PC time.
        /// </summary>
        private DateTime _time0;

        /// <summary>
        /// Frame ID (counts number of frames generated since opening camera).
        /// </summary>
        private uint _frameID;

        /// <summary>
        /// Counts number of frames since acquisition start.
        /// </summary>
        private uint _frameCounter;

        /// <summary>
        /// List of image paths in image directory.
        /// </summary>
        private List<string> _framePaths;

        #endregion

        #region Constructors

        /// <summary>
        /// Creates a new GenApi module attached to camera.
        /// </summary>
        /// <param name="virtualCam">Camera.</param>
        public GenApi(VirtualCam virtualCam)
        {
            _virtualCam = virtualCam;

            // Start internal camera clock.
            _cameraClock = new Stopwatch();
            _time0 = DateTime.Now;
            _cameraClock.Start();

            // Reset frame counters.
            _frameID = 0;
            _frameCounter = 0;

            // Initialize GenApi parameters/features.
            InitializeParameters();

            // Warm-up ImagePatternGenerator.
            _ = ImagePatternGenerator.CreateImage((uint)Width, (uint)Height, (PixelFormat)PixelFormat.IntValue, (TestPattern)TestPattern.IntValue);

            // Subscribe to device events.
            _virtualCam.ParameterInvalidate += OnParameterChanged;
        }

        #endregion

        #region Public methods

        /// <inheritdoc/>
        public void Dispose()
        {
            // Stop internal camera clock.
            if (_cameraClock.IsRunning)
                _cameraClock.Stop();

            // Stop image generator timer.
            _imagePatternGeneratorTimer?.Stop();
            _imagePatternGeneratorTimer?.Dispose();

            // Unsubscribe from device events.
            _virtualCam.ParameterInvalidate -= OnParameterChanged;
        }

        /// <summary>
        /// Retrieves lists of parameters implemented by camera.
        /// </summary>
        /// <param name="parameterList">List of successfully imported parameters and features implemented by camera.</param>
        /// <param name="failedParameterList">List of unsuccessfully imported parameters and features implemented by camera.</param>
        public void GetParameterList(out List<GcParameter> parameterList, out List<string> failedParameterList)
        {
            parameterList = [];
            failedParameterList = [];

            // Get properties (using Reflection).
            PropertyInfo[] propertyInfos = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

            // Add GcParameter type properties to parameterList.
            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.GetValue(this) is GcParameter gcParameter && gcParameter.IsImplemented)
                    parameterList.Add(gcParameter);
            }
        }

        /// <summary>
        /// Responds to changes of a GenApi parameter, relaying them to device and updating dependent parameters as necessary.
        /// </summary>
        /// <param name="eventArgs">Event arguments containing name of parameter that was changed.</param>
        public void OnParameterChanged(object sender, ParameterInvalidateEventArgs eventArgs)
        {
            string parameterName = eventArgs.ParameterName;

            // Update parameter dependencies.

            if (parameterName == nameof(Width))
            {
                OffsetX.ImposeMax(WidthMax - Width);
            }

            if (parameterName == nameof(Height))
            {
                OffsetY.ImposeMax(HeightMax - Height);
            }

            if (parameterName == nameof(BinningHorizontal))
            {
                var previousBinning = SensorWidth / WidthMax;
                var previousWidth = Width;
                var previousOffsetX = OffsetX;

                WidthMax.Value = SensorWidth / BinningHorizontal;
                if (previousBinning / BinningHorizontal < 1) // binning down
                {
                    Width.Value = previousWidth * previousBinning / BinningHorizontal;
                    Width.ImposeMax(WidthMax);
                    OffsetX.Value = previousOffsetX * previousBinning / BinningHorizontal;
                    OffsetX.ImposeMax(WidthMax - Width);
                }
                else // binning up
                {
                    Width.ImposeMax(WidthMax);
                    Width.Value = previousWidth * previousBinning / BinningHorizontal;
                    OffsetX.ImposeMax(WidthMax - Width);
                    OffsetX.Value = previousOffsetX * previousBinning / BinningHorizontal;
                }
            }

            if (parameterName == nameof(BinningVertical))
            {
                var previousBinning = SensorHeight / HeightMax;
                var previousHeight = Height;
                var previousOffsetY = OffsetY;

                HeightMax.Value = SensorHeight / BinningVertical;
                if (previousBinning / BinningVertical < 1) // binning down
                {
                    Height.Value = previousHeight * previousBinning / BinningVertical;
                    Height.ImposeMax(HeightMax);
                    OffsetY.Value = previousOffsetY * previousBinning / BinningVertical;
                    OffsetY.ImposeMax(HeightMax - Height);
                }
                else // binning up
                {
                    Height.ImposeMax(HeightMax);
                    Height.Value = previousHeight * previousBinning / BinningVertical;
                    OffsetY.ImposeMax(HeightMax - Height);
                    OffsetY.Value = previousOffsetY * previousBinning / BinningVertical;
                }
            }

            if (parameterName == nameof(PixelFormat))
            {
                PixelSize.IntValue = (int)GenICamConverter.GetPixelSize((PixelFormat)PixelFormat.IntValue);
            }

            if (parameterName == nameof(DeviceTemperatureSelector))
            {
                // Change temperature according to selected sensor.
                switch (DeviceTemperatureSelector.StringValue)
                {
                    case "Sensor":
                        DeviceTemperature.Value = 34.1;
                        break;

                    case "Mainboard":
                        DeviceTemperature.Value = 23.3;
                        break;

                    case "DeviceSpecific":
                        DeviceTemperature.Value = 20.0;
                        break;
                }
            }
        }

        #endregion
    }
}