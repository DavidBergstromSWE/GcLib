using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Timers;
using Emgu.CV;
using Emgu.CV.Stitching;
using GcLib.Utilities.Imaging;
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
        public GcEnumeration AcquisitionMode => _acquisitionMode;

        /// <summary>
        /// Number of images to acquire for a <see cref="AcquisitionMode.MultiFrame"/> acquisition mode.
        /// </summary>
        [Category("AcquisitionControl")]
        [DefaultValue(10)]
        public GcInteger AcquisitionFrameCount => _acquisitionFrameCount;

        /// <summary>
        /// Upper limit for acquisition rate at which frames are captured.
        /// </summary>
        [Category("AcquisitionControl")]
        [DefaultValue(30)]
        public GcFloat AcquisitionFrameRate => _acquisitionFrameRate;

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
                    _testPatternGeneratorTimer.Elapsed += OnTimerElapsed; // register to event using eventhandler OnTimerElapsed
                    _testPatternGeneratorTimer.Start(); // start image generation

                    _virtualCam.IsAcquiring = true;

                    // Log debugging info.
                    if (GcLibrary.Logger.IsEnabled(LogLevel.Trace))
                        GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", _virtualCam.DeviceInfo.ModelName, _virtualCam.DeviceInfo.UniqueID);

                    _virtualCam.OnAcquisitionStarted();

                    // Simulate hardware failure (from different thread).
                    if (AcquisitionFailure)
                    {
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay((int)AcquisitionTimeToFailure.Value);
                            if (AcquisitionFailureEvent.IntValue == (int)AcquisitionFailureEventType.ConnectionLost)
                                _virtualCam.OnConnectionLost();
                            else
                                _virtualCam.OnAcquisitionAborted(new AcquisitionAbortedEventArgs("Acquisition aborted due to hardware failure!"));
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
                    if (_virtualCam.IsAcquiring == false)
                        return;

                    _testPatternGeneratorTimer.Elapsed -= OnTimerElapsed; // unregister from event
                    _testPatternGeneratorTimer.Stop();
                    _testPatternGeneratorTimer.Dispose();

                    _virtualCam.IsAcquiring = false;

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
            GcBuffer buffer;

            if (TestPattern.StringValue == GcLib.TestPattern.ImageDirectory.ToString())
            {
                if (_frameCounter <= _framePaths.Count - 1)
                {
                    // Load image from directory.
                    var mat = new Mat(_framePaths[(int)_frameCounter], Emgu.CV.CvEnum.ImreadModes.Unchanged);
                    buffer = new GcBuffer(mat, (uint)EmguConverter.GetMax(mat.Depth), _frameCounter++, (ulong)TimeStamp.Value);
                }
                else
                {
                    // Rewind and start from beginning if in continuous mode.
                    if (AcquisitionMode.StringValue == GcLib.AcquisitionMode.Continuous.ToString())
                        _frameCounter = 0;
                    else AcquisitionStop.Execute(); // No more images to read, stop acquisition.

                    return;
                }
            }
            else
            {
                // Create image data using test pattern generator.
                var imageData = TestPatternGenerator.CreateImage(width: (uint)Width.Value,
                                                                 height: (uint)Height.Value,
                                                                 pixelFormat: (PixelFormat)PixelFormat.IntValue,
                                                                 testPattern: (TestPattern)TestPattern.IntValue,
                                                                 frameNumber: _frameCounter++);

                // Apply binning (if selected).
                if (BinningHorizontal.Value > 1 || BinningVertical.Value > 1)
                {
                    var mat = new Mat((int)Height.Value, (int)Width.Value, EmguConverter.GetDepthType((PixelFormat)PixelFormat.IntValue), (int)GenICamHelper.GetNumChannels((PixelFormat)PixelFormat.IntValue));
                    mat.SetTo(imageData);
                    CvInvoke.BoxFilter(mat, mat, EmguConverter.GetDepthType((PixelFormat)PixelFormat.IntValue), new Size((int)BinningHorizontal.Value, (int)BinningVertical.Value), new Point(-1, -1));
                    CvInvoke.Resize(mat, mat, new Size((int)Width.Value / (int)BinningHorizontal.Value, (int)Height.Value / (int)BinningVertical.Value));
                    buffer = new GcBuffer(imageMat: mat,
                                          pixelFormat: (PixelFormat)PixelFormat.IntValue,
                                          pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax((PixelFormat)PixelFormat.IntValue),
                                          frameID: _frameCounter,
                                          timeStamp: (ulong)TimeStamp.Value);
                }
                else buffer = new GcBuffer(imageData: imageData,
                                           width: (uint)Width.Value,
                                           height: (uint)Height.Value,
                                           pixelFormat: (PixelFormat)PixelFormat.IntValue,
                                           pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax((PixelFormat)PixelFormat.IntValue),
                                           frameID: _frameCounter,
                                           timeStamp: (ulong)TimeStamp.Value);
            }

            // Raise new buffer event.
            _virtualCam.OnNewBuffer(new NewBufferEventArgs(buffer, DateTime.Now));

            // Check if acquisition is done.
            if ((AcquisitionMode.StringValue == GcLib.AcquisitionMode.SingleFrame.ToString()) || (AcquisitionMode.StringValue == GcLib.AcquisitionMode.MultiFrame.ToString() && _frameCounter == AcquisitionFrameCount.Value))
            {
                // Acquisition is done, stop.
                AcquisitionStop.Execute();
                return;
            }
        }

        #endregion
    }
}