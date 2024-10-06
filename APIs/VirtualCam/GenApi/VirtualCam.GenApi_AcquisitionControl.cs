﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;
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

                        _framePaths = Directory.EnumerateFiles(ImageDirectory, "*.*", SearchOption.TopDirectoryOnly)
                                               .Where(s => s.EndsWith(".bmp") || s.EndsWith(".png"))
                                               .ToList();
                        if (_framePaths.Count == 0)
                            throw new FileNotFoundException($"No image files of type bmp or png found in '{ImageDirectory}'!");
                    }

                    _imagePatternGeneratorTimer = new Timer(1 / AcquisitionFrameRate * 1000) { AutoReset = true };
                    _imagePatternGeneratorTimer.Elapsed += OnTimerElapsed; // register to event using eventhandler OnTimerElapsed
                    _imagePatternGeneratorTimer.Start(); // start image generation

                    _virtualCam.IsAcquiring = true;

                    // Log debugging info.
                    GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) started", _virtualCam.DeviceInfo.ModelName, _virtualCam.DeviceInfo.UniqueID);

                    _virtualCam.OnAcquisitionStarted();

                    // Simulate hardware failure (from different thread).
                    if (AcquisitionFailure)
                    {
                        System.Threading.Tasks.Task.Run(async () =>
                        {
                            await System.Threading.Tasks.Task.Delay((int)AcquisitionTimeToFailure);
                            if (AcquisitionFailureEvent.IntValue == (int)AcquisitionFailureEventType.ConnectionLost)
                                _virtualCam.OnConnectionLost();
                            else
                            {
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
                    if (_virtualCam.IsAcquiring == false)
                        return;

                    _imagePatternGeneratorTimer.Elapsed -= OnTimerElapsed; // unregister from event
                    _imagePatternGeneratorTimer.Stop();
                    _imagePatternGeneratorTimer.Dispose();

                    _virtualCam.IsAcquiring = false;

                    // Log debugging info.
                    GcLibrary.Logger.LogTrace("Image acquisition thread in Device {ModelName} (ID: {ID}) stopped", _virtualCam.DeviceInfo.ModelName, _virtualCam.DeviceInfo.UniqueID);

                    _virtualCam.OnAcquisitionStopped();
                });
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Converts image byte array to <see cref="GcBuffer"/>.
        /// </summary>
        /// <param name="buffer">Image buffer as byte array.</param>
        /// <returns><see cref="GcBuffer"/> with image and chunkdata.</returns>
        private GcBuffer ToGcBuffer(byte[] buffer)
        {
            return new GcBuffer(imageData: buffer,
                                width: (uint)Width,
                                height: (uint)Height,
                                pixelFormat: (PixelFormat)PixelFormat.IntValue,
                                pixelDynamicRangeMax: GenICamConverter.GetDynamicRangeMax((PixelFormat)PixelFormat.IntValue),
                                frameID: _frameID++,
                                timeStamp: (ulong)TimeStamp.Value);
        }

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
                    var mat = new Emgu.CV.Mat(_framePaths[(int)_frameCounter++], Emgu.CV.CvEnum.ImreadModes.Unchanged);
                    buffer = new GcBuffer(mat, (uint)EmguConverter.GetMax(mat.Depth), _frameID++, (ulong)TimeStamp.Value);
                }
                else
                {
                    AcquisitionStop.Execute();
                    return;
                }
            }
            else
            {
                // Generate image with width, height, pixel format and test pattern according to set properties.
                buffer = ToGcBuffer(ImagePatternGenerator.CreateImage(width: (uint)Width,
                                                                      height: (uint)Height,
                                                                      pixelFormat: (PixelFormat)PixelFormat.IntValue,
                                                                      testPattern: (TestPattern)TestPattern.IntValue,
                                                                      frameNumber: ++_frameCounter));
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