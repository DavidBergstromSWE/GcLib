using System;
using Emgu.CV;
using Emgu.CV.Structure;
using GcLib.Utilities.Imaging;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GcLib;

public sealed partial class VirtualCam
{
    private sealed partial class GenApi
    {
        /// <summary>
        /// Handles frame source and transform pipeline for VirtualCam acquisition.
        /// </summary>
        private sealed class FramePipeline(GenApi owner)
        {
            /// <summary>
            /// The owning <see cref="GenApi"/> instance.
            /// </summary>
            private readonly GenApi _owner = owner;

            /// <summary>
            /// Creates a buffer from an image loaded from the configured directory source.
            /// ROI settings are intentionally ignored for directory-backed images.
            /// </summary>
            /// <param name="frameId">The frame ID.</param>
            /// <param name="binningHorizontal">The horizontal binning factor.</param>
            /// <param name="binningVertical">The vertical binning factor.</param>
            /// <param name="reverseX">If true, flip horizontally.</param>
            /// <param name="reverseY">If true, flip vertically.</param>
            public GcBuffer CreateBufferFromDirectoryFrame(long frameId, int binningHorizontal, int binningVertical, bool reverseX, bool reverseY)
            {
                using var mat = new Mat(_owner._framePaths[(int)frameId], Emgu.CV.CvEnum.ImreadModes.Unchanged);
                var detectedPixelFormat = EmguHelper.GetPixelFormat(mat.Depth, mat.NumberOfChannels);
                if (detectedPixelFormat == GcLib.PixelFormat.InvalidPixelFormat)
                    throw new NotSupportedException($"Pixel format {detectedPixelFormat} is not supported!");

                ApplyBinning(mat, binningHorizontal, binningVertical);
                ApplyFlip(mat, reverseX, reverseY);

                // You can check at runtime whether a Mat has row padding by inspecting mat.IsContinuous.
                // If mat.IsContinuous is true, there is zero row padding.
                // If false, the exact byte distance between rows is stored in mat.Step(in bytes), whereas the active pixel data per row is mat.Cols * mat.ElementSize.

                return new GcBuffer(mat, detectedPixelFormat, frameId, (ulong)_owner.TimeStamp.Value);
            }

            /// <summary>
            /// Creates a buffer from generated test-pattern image data.
            /// </summary>
            /// <param name="frameId">The frame ID.</param>
            /// <param name="offsetX">The X offset of the ROI.</param>
            /// <param name="offsetY">The Y offset of the ROI.</param>
            /// <param name="width">The width of the ROI.</param>
            /// <param name="height">The height of the ROI.</param>
            /// <param name="widthMax">The maximum width of the generated image.</param>
            /// <param name="heightMax">The maximum height of the generated image.</param>
            /// <param name="reverseY">If true, flip vertically.</param>
            /// <param name="reverseX">If true, flip horizontally.</param>
            public GcBuffer CreateBufferFromGeneratedFrame(long frameId, int offsetX, int offsetY, int width, int height, int widthMax, int heightMax, bool reverseX, bool reverseY)
            {
                var configuredPixelFormat = (PixelFormat)_owner.PixelFormat.IntValue;
                var imageData = TestPatternGenerator.CreateImage(width: (uint)widthMax,
                                                                 height: (uint)heightMax,
                                                                 pixelFormat: configuredPixelFormat,
                                                                 testPattern: (TestPattern)_owner.TestPattern.IntValue,
                                                                 frameNumber: (ulong)frameId);

                if (offsetX == 0 && offsetY == 0 && width == widthMax && height == heightMax && reverseX == false && reverseY == false)
                {
                    return new GcBuffer(imageData,
                                        (uint)widthMax,
                                        (uint)heightMax,
                                        configuredPixelFormat,
                                        frameId,
                                        (ulong)_owner.TimeStamp.Value);
                }

                using var mat = new Mat(heightMax,
                                        widthMax,
                                        EmguHelper.GetDepthType(configuredPixelFormat),
                                        (int)GenICamHelper.GetNumChannels(configuredPixelFormat));
                mat.SetTo(imageData);

                return CreateTransformedBuffer(mat, configuredPixelFormat, frameId, offsetX, offsetY, width, height, reverseX, reverseY);
            }

            /// <summary>
            /// Applies configured ROI and transforms, then converts the result to a <see cref="GcBuffer"/>.
            /// </summary>
            /// <param name="mat">The source Mat.</param>
            /// <param name="pixelFormat">The pixel format of the source Mat.</param>
            /// <param name="frameId">The frame ID.</param>
            /// <param name="offsetX">The X offset of the ROI.</param>
            /// <param name="offsetY">The Y offset of the ROI.</param>
            /// <param name="width">The width of the ROI.</param>
            /// <param name="height">The height of the ROI.</param>
            /// <param name="reverseX">If true, flip horizontally.</param>
            /// <param name="reverseY">If true, flip vertically.</param>
            private GcBuffer CreateTransformedBuffer(Mat mat, PixelFormat pixelFormat, long frameId, int offsetX, int offsetY, int width, int height, bool reverseX, bool reverseY)
            {
                using var roiMat = CreateRoiMat(mat, offsetX, offsetY, width, height);
                ApplyFlip(roiMat, reverseX, reverseY);

                return new GcBuffer(roiMat, pixelFormat, frameId, (ulong)_owner.TimeStamp.Value);
            }

            /// <summary>
            /// Creates a Mat view representing the configured ROI from the source image.
            /// </summary>
            /// <param name="mat">The source Mat.</param>
            /// <param name="offsetX">The X offset of the ROI.</param>
            /// <param name="offsetY">The Y offset of the ROI.</param>
            /// <param name="width">The width of the ROI.</param>
            /// <param name="height">The height of the ROI.</param>
            private static Mat CreateRoiMat(Mat mat, int offsetX, int offsetY, int width, int height)
            {
                if (offsetX == 0 && offsetY == 0 && width == mat.Width && height == mat.Height)
                    return mat;

                var clampedOffsetX = Math.Clamp(offsetX, 0, Math.Max(0, mat.Width - 1));
                var clampedOffsetY = Math.Clamp(offsetY, 0, Math.Max(0, mat.Height - 1));
                var clampedWidth = Math.Clamp(width, 1, mat.Width - clampedOffsetX);
                var clampedHeight = Math.Clamp(height, 1, mat.Height - clampedOffsetY);

                return new Mat(mat, new System.Drawing.Rectangle(clampedOffsetX, clampedOffsetY, clampedWidth, clampedHeight));
            }

            /// <summary>
            /// Applies image binning using box-filter aggregation and downsampling.
            /// </summary>
            /// <param name="mat">The Mat to apply binning to.</param>
            /// <param name="binningHorizontal">The horizontal binning factor.</param>
            /// <param name="binningVertical">The vertical binning factor.</param>
            private static void ApplyBinning(Mat mat, int binningHorizontal, int binningVertical)
            {
                if (binningHorizontal <= 1 && binningVertical <= 1)
                    return;

                CvInvoke.BoxFilter(mat, mat, mat.Depth, new System.Drawing.Size(binningHorizontal, binningVertical), new System.Drawing.Point(-1, -1));
                CvInvoke.Resize(mat, mat, new System.Drawing.Size(Math.Max(1, mat.Width / binningHorizontal), Math.Max(1, mat.Height / binningVertical)));
            }

            /// <summary>
            /// Applies horizontal/vertical flipping based on the configured reverse flags.
            /// </summary>
            /// <param name="mat">The Mat to flip.</param>
            /// <param name="reverseX">If true, flip horizontally.</param>
            /// <param name="reverseY"
            private static void ApplyFlip(Mat mat, bool reverseX, bool reverseY)
            {
                if (reverseX == false && reverseY == false)
                    return;

                var flipType = reverseX && reverseY
                    ? Emgu.CV.CvEnum.FlipType.Both
                    : reverseX
                        ? Emgu.CV.CvEnum.FlipType.Horizontal
                        : Emgu.CV.CvEnum.FlipType.Vertical;

                CvInvoke.Flip(mat, mat, flipType);
            }
        }
    }
}
