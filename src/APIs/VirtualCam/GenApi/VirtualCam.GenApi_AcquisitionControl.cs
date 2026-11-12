using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;
using Microsoft.Extensions.Logging;

namespace GcLib;

public sealed partial class VirtualCam
{
    /// <summary>
    /// Nested class containing all publicly exposed GenApi device parameters.
    /// </summary>
    private sealed partial class GenApi : IDisposable
    {
        #region Enums

        /// <summary>
        /// Type of event to use for simulating hardware failure during acquisition.
        /// </summary>
        enum AcquisitionFailureEventType
        {
            /// <summary>
            /// Event signalling a lost connection to device.
            /// </summary>
            ConnectionLost,
            /// <summary>
            /// Event signalling disrupted acquisition due to errors during image capture.
            /// </summary>
            AcquisitionAborted
        }

        #endregion

        #region Backing fields

        private GcEnumeration _acquisitionMode;
        private GcInteger _acquisitionFrameCount;
        private GcFloat _acquisitionFrameRate;

        #endregion

        // Provides properties for acquisition control.

        #region Acquisition Control

        /// <summary>
        /// Acquisition mode.
        /// </summary>
        [Category("AcquisitionControl")]
        [DefaultValue(GcLib.AcquisitionMode.Continuous)]
        public GcEnumeration AcquisitionMode => _acquisitionMode ??= new GcEnumeration(
            name: nameof(AcquisitionMode),
            category: GetCategory(nameof(AcquisitionMode)),
            description: "Sets the acquisition mode of the device",
            enumEntry: GcLib.AcquisitionMode.Continuous,
            enumType: typeof(AcquisitionMode));

        /// <summary>
        /// Number of images to acquire for a <see cref="AcquisitionMode.MultiFrame"/> acquisition mode.
        /// </summary>
        [Category("AcquisitionControl")]
        [DefaultValue(10)]
        public GcInteger AcquisitionFrameCount => _acquisitionFrameCount ??= new GcInteger(
            name: nameof(AcquisitionFrameCount),
            category: GetCategory(nameof(AcquisitionFrameCount)),
            description: $"Number of frames to acquire when {nameof(AcquisitionMode)} is set to {nameof(GcLib.AcquisitionMode.MultiFrame)}",
            value: 10,
            min: 1,
            max: 30,
            increment: 1,
            incrementMode: EIncMode.fixedIncrement,
            unit: "");

        /// <summary>
        /// Upper limit for acquisition rate at which frames are captured.
        /// </summary>
        [Category("AcquisitionControl")]
        [DefaultValue(30)]
        public GcFloat AcquisitionFrameRate => _acquisitionFrameRate ??= new GcFloat(
            name: nameof(AcquisitionFrameRate),
            category: GetCategory(nameof(AcquisitionFrameRate)),
            description: "Controls the acquisition rate (in Hertz) at which the frames are captured",
            value: 30,
            min: 1,
            max: 60,
            increment: 0.1,
            displayPrecision: 1,
            unit: "Hz");

        /// <summary>
        /// Arms (prepares) the camera for acquisition.
        /// </summary>
        //[Category("AcquisitionControl")]
        //public GcCommand AcquisitionArm
        //{
        //    get => new(
        //       name: "AcquisitionArm",
        //       category: "AcquisitionControl",
        //       description: "Arms the device before an AcquisitionStart command",
        //       method: () =>
        //       {
        //           // Pre-allocate?
        //       },
        //       visibility: GcVisibility.Expert);
        //}

        /// <summary>
        /// Provides a command to start an acquisition.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcCommand AcquisitionStart
        {
            get => new(
                name: "AcquisitionStart",
                category: "AcquisitionControl",
                description: "Starts acquisition on the device",
                method: () =>
                {
                    lock (_acquisitionSync)
                    {
                        if (_virtualCam.IsAcquiring)
                            throw new InvalidOperationException($"Unable to start acquisition as Device {_virtualCam.DeviceInfo.ModelName} is already acquiring!");

                        // Reset frame counter.
                        _frameCounter = 0;

                        // Read images from directory.
                        if (TestPattern.StringValue == GcLib.TestPattern.ImageDirectory.ToString())
                        {
                            if (string.IsNullOrEmpty(ImageDirectory))
                                throw new ArgumentException($"{nameof(ImageDirectory)} is not specified!");

                            if (Directory.Exists(ImageDirectory) == false)
                                throw new DirectoryNotFoundException($"Directory '{ImageDirectory}' not found!");

                            _framePaths = [.. Directory.EnumerateFiles(ImageDirectory, "*.*", SearchOption.TopDirectoryOnly).Where(s => s.EndsWith(".bmp") || s.EndsWith(".png"))];
                            if (_framePaths.Count == 0)
                                throw new FileNotFoundException($"No image files of type bmp or png found in '{ImageDirectory}'!");
                        }

                        _testPatternGeneratorTimer = new Timer(1 / AcquisitionFrameRate * 1000) { AutoReset = true };
                        _testPatternGeneratorTimer.Elapsed += OnTimerElapsed;
                        _testPatternGeneratorTimer.Start();

                        _virtualCam.IsAcquiring = true;
                    }

                    // Log debugging info.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
                        GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", _virtualCam.DeviceInfo.ModelName, _virtualCam.DeviceInfo.UniqueID);

                    _virtualCam.OnAcquisitionStarted();

                    // Simulate hardware failure (from different thread).
                    if (AcquisitionFailure)
                    {
                        _ = System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay((int)AcquisitionTimeToFailure.Value);

                            lock (_acquisitionSync)
                            {
                                if (_virtualCam.IsAcquiring == false)
                                    return;

                                if (AcquisitionFailureEvent.IntValue == (int)AcquisitionFailureEventType.ConnectionLost)
                                    _virtualCam.OnConnectionLost();
                                else
                                    _virtualCam.OnAcquisitionAborted(new AcquisitionAbortedEventArgs("Acquisition aborted due to hardware failure!"));
                            }
                        });
                    }
                });
        }

        /// <summary>
        /// Provides a command to stop an acquisition.
        /// </summary>
        [Category("AcquisitionControl")]
        public GcCommand AcquisitionStop
        {
            get => new(
                name: "AcquisitionStop",
                category: "AcquisitionControl",
                description: "Stops acquisition on the device",
                method: () =>
                {
                    lock (_acquisitionSync)
                    {
                        if (_virtualCam.IsAcquiring == false)
                            return;

                        _testPatternGeneratorTimer.Elapsed -= OnTimerElapsed;
                        _testPatternGeneratorTimer.Stop();
                        _testPatternGeneratorTimer.Dispose();
                        _testPatternGeneratorTimer = null;

                        _virtualCam.IsAcquiring = false;
                    }

                    // Log debugging info.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
                        GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) stopped", _virtualCam.DeviceInfo.ModelName, _virtualCam.DeviceInfo.UniqueID);

                    _virtualCam.OnAcquisitionStopped();
                });
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Event-handling method to Elapsed events in Timer object. Generates images and signals new buffer events.
        /// </summary>
        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                GcBuffer buffer = null;
                bool shouldStopAfterFrame = false;

                lock (_acquisitionSync)
                {
                    if (_virtualCam.IsAcquiring == false)
                        return;

                    var frameId = _frameCounter;
                    var binningHorizontal = (int)BinningHorizontal.Value;
                    var binningVertical = (int)BinningVertical.Value;
                    var reverseX = ReverseX.Value;
                    var reverseY = ReverseY.Value;
                    var offsetX = (int)OffsetX.Value;
                    var offsetY = (int)OffsetY.Value;
                    var width = (int)Width.Value;
                    var height = (int)Height.Value;
                    var widthMax = (int)WidthMax.Value;
                    var heightMax = (int)HeightMax.Value;

                    if (TestPattern.StringValue == GcLib.TestPattern.ImageDirectory.ToString())
                    {
                        if (_frameCounter > _framePaths.Count - 1)
                        {
                            // Rewind and start from beginning if in continuous mode.
                            if (AcquisitionMode.StringValue == GcLib.AcquisitionMode.Continuous.ToString())
                            {
                                _frameCounter = 0;
                                return;
                            }

                            // No more images to read in finite acquisition mode.
                            shouldStopAfterFrame = true;
                        }
                        else
                        {
                            // Images read from directory are assumed to be tightly packed in memory (no row padding). Is this assumption valid for all image formats? If not, we need to check for row padding and handle it accordingly.
                            buffer = _framePipeline.CreateBufferFromDirectoryFrame(frameId, binningHorizontal, binningVertical, reverseX, reverseY);
                        }
                    }
                    else
                    {
                        // TestPatternGenerator packs data tightly in memory, so we can assume that image data is tightly packed (no row padding).
                        buffer = _framePipeline.CreateBufferFromGeneratedFrame(frameId, offsetX, offsetY, width, height, widthMax, heightMax, reverseX, reverseY);
                    }

                    if (buffer is not null)
                    {
                        _frameCounter++;
                        shouldStopAfterFrame = (AcquisitionMode.StringValue == GcLib.AcquisitionMode.SingleFrame.ToString())
                                               || (AcquisitionMode.StringValue == GcLib.AcquisitionMode.MultiFrame.ToString() && _frameCounter == AcquisitionFrameCount.Value);
                    }
                }

                // Raise new buffer event outside lock.
                if (buffer is not null)
                    _virtualCam.OnNewBuffer(new NewBufferEventArgs(string.Empty, DateTime.Now, buffer));

                if (shouldStopAfterFrame)
                    AcquisitionStop.Execute();
            }
            catch (Exception ex)
            {
                if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                    GcLibrary.Logger.LogError(ex, "Acquisition callback failed in Device {ModelName} (ID: {ID})", _virtualCam.DeviceInfo.ModelName, _virtualCam.DeviceInfo.UniqueID);

                bool wasAcquiring;
                lock (_acquisitionSync)
                    wasAcquiring = _virtualCam.IsAcquiring;

                if (wasAcquiring)
                {
                    AcquisitionStop.Execute();
                    _virtualCam.OnAcquisitionAborted(new AcquisitionAbortedEventArgs("Acquisition failed in VirtualCam timer callback.", ex));
                }
            }
        }

        #endregion
    }
}