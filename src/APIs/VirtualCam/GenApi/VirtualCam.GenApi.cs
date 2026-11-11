using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// Timer object (event generator) for the image test pattern generator.
        /// </summary>
        private Timer _testPatternGeneratorTimer;

        /// <summary>
        /// DateTime object for translating camera clock to PC time.
        /// </summary>
        private DateTime _time0;

        /// <summary>
        /// Counts number of frames since acquisition start.
        /// </summary>
        private uint _frameCounter;

        /// <summary>
        /// List of image paths in image directory.
        /// </summary>
        private List<string> _framePaths;

        /// <summary>
        /// Handles frame-source and transform-to-buffer pipeline logic.
        /// </summary>
        private readonly FramePipeline _framePipeline;

        /// <summary>
        /// Handles updates of derived parameter values and constraints.
        /// </summary>
        private readonly DerivedParameterUpdater _derivedParameterUpdater;

        /// <summary>
        /// Synchronization object for acquisition start/stop/frame callback.
        /// </summary>
        private readonly object _acquisitionSync = new();

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

            _framePipeline = new FramePipeline(this);
            _derivedParameterUpdater = new DerivedParameterUpdater(this);

            // Reset frame counters.
            _frameCounter = 0;

            // Warm-up image test pattern generator.
            // Accessing these properties initializes required GenApi parameters lazily.
            _ = TestPatternGenerator.CreateImage((uint)Width.Value, (uint)Height.Value, (PixelFormat)PixelFormat.IntValue, (TestPattern)TestPattern.IntValue);

            // Ensure all derived parameter values are deterministic after lazy initialization.
            _derivedParameterUpdater.Update();

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
            _testPatternGeneratorTimer?.Stop();
            _testPatternGeneratorTimer?.Dispose();

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
            _derivedParameterUpdater.Update(eventArgs.ParameterName);
        }

        #endregion

        #region Private methods

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