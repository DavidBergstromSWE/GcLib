using System;
using GcLib.Utilities.Imaging;

namespace GcLib.UnitTests.Utilities
{
    /// <summary>
    /// Provides fake buffers for e.g. unit testing purposes.
    /// </summary>
    internal static class FakeBufferProvider
    {
        /// <summary>
        /// Running frame ID.
        /// </summary>
        static long _frameID = 0;

        /// <summary>
        /// Creates a new fake <see cref="GcBuffer"/> object.
        /// </summary>
        /// <returns>Fake buffer.</returns>
        public static GcBuffer GetFakeBuffer()
        {
            return GetFakeBuffer(3, 3, PixelFormat.Mono8, ++_frameID);
        }

        /// <summary>
        /// Creates a new fake <see cref="GcBuffer"/> object, with specified buffer ID.
        /// </summary>
        /// <param name="frameID">Buffer ID.</param>
        /// <returns>Fake buffer.</returns>
        public static GcBuffer GetFakeBuffer(long frameID)
        {
            return GetFakeBuffer(3, 3, PixelFormat.Mono8, frameID);
        }

        /// <summary>
        /// Creates a new fake <see cref="GcBuffer"/> object, with specified width, height and pixel format.
        /// </summary>
        /// <param name="width">Width of buffer.</param>
        /// <param name="height">Height of buffer.</param>
        /// <param name="pixelFormat">Pixel format of buffer.</param>
        /// <returns>Fake buffer.</returns>
        public static GcBuffer GetFakeBuffer(uint width, uint height, PixelFormat pixelFormat)
        {
            return GetFakeBuffer(width, height, pixelFormat, ++_frameID);
        }

        /// <summary>
        /// Creates a new fake <see cref="GcBuffer"/> object, with specified width, height, pixel format and frame ID.
        /// </summary>
        /// <param name="width">Width of buffer.</param>
        /// <param name="height">Height of buffer.</param>
        /// <param name="pixelFormat">Pixel format of buffer.</param>
        /// <param name="frameID">Buffer ID.</param>
        /// <returns>Fake buffer.</returns>
        public static GcBuffer GetFakeBuffer(uint width, uint height, PixelFormat pixelFormat, long frameID)
        {
            return new(imageData: TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.FrameCounter, (ulong)frameID),
                       width: width,
                       height: height,
                       pixelFormat: pixelFormat,
                       pixelDynamicRangeMax: GenICamConverter.GetDynamicRangeMax(pixelFormat),
                       frameID: frameID,
                       timeStamp: (ulong)DateTime.Now.Ticks);
        }

        /// <summary>
        /// Creates a new fake <see cref="GcBuffer"/> object, with specified width, height, pixel format, frame ID and timestamp.
        /// </summary>
        /// <param name="width">Width of buffer.</param>
        /// <param name="height">Height of buffer.</param>
        /// <param name="pixelFormat">Pixel format of buffer.</param>
        /// <param name="frameID">Buffer ID.</param>
        /// <param name="timeStamp">Buffer timestamp.</param>
        /// <returns>Fake buffer.</returns>
        public static GcBuffer GetFakeBuffer(uint width, uint height, PixelFormat pixelFormat, long frameID, ulong timeStamp)
        {
            return new(imageData: TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.FrameCounter, (ulong)frameID),
                       width: width,
                       height: height,
                       pixelFormat: pixelFormat,
                       pixelDynamicRangeMax: GenICamConverter.GetDynamicRangeMax(pixelFormat),
                       frameID: frameID,
                       timeStamp: timeStamp);
        }
    }
}