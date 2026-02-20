using System;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests
{
    [TestClass]
    public class GcBufferTests
    {
        #region ConstructorTests

        [TestMethod]
        [DataRow(PixelFormat.Mono8)]
        [DataRow(PixelFormat.Mono10)]
        [DataRow(PixelFormat.Mono12)]
        [DataRow(PixelFormat.Mono14)]
        [DataRow(PixelFormat.Mono16)]
        [DataRow(PixelFormat.RGB8)]
        public void GcBuffer_ByteArrayWithValidInputs_IsNotNull(PixelFormat pixelFormat)
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);

            // Act
            var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);

            // Assert
            Assert.IsNotNull(buffer);
        }

        [TestMethod]
        [DataRow(PixelFormat.Mono8, 2, 3)]
        [DataRow(PixelFormat.Mono10, 2, 2)]
        [DataRow(PixelFormat.Mono12, 3, 2)]
        [DataRow(PixelFormat.Mono14, 2, 3)]
        [DataRow(PixelFormat.Mono16, 3, 2)]
        [DataRow(PixelFormat.RGB8, 3, 3)]
        public void GcBuffer_ByteArrayWithValidInputs_PropertiesAreEqual(PixelFormat pixelFormat, int width, int height)
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage((uint)width, (uint)height, pixelFormat, TestPattern.GrayHorizontalRamp);
            var frameID = Random.Shared.Next();
            var timeStamp = (ulong)DateTime.Now.Ticks;

            // Act
            var buffer = new GcBuffer(imageData, (uint)width, (uint)height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), frameID, timeStamp);

            // Assert
            Assert.IsTrue(Enumerable.SequenceEqual(imageData, buffer.ImageData)); // checks element equality
            Assert.AreEqual((uint)height, buffer.Height);
            Assert.AreEqual((uint)width, buffer.Width);
            Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), buffer.PixelDynamicRangeMax);
            Assert.AreEqual<uint>(0, buffer.PixelDynamicRangeMin);
            Assert.AreEqual(frameID, buffer.FrameID);
            Assert.AreEqual(timeStamp, buffer.TimeStamp);
        }

        [TestMethod]
        public void GcBuffer_ByteArrayWithValidInputs_AreSame()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);

            // Act
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Assert
            Assert.AreSame(expected: imageData, actual: buffer.ImageData); // checks reference equality (share same memory)
            Assert.IsTrue(Enumerable.SequenceEqual(imageData, buffer.ImageData)); // checks pixel data equality
        }

        [TestMethod]
        public void GcBuffer_ByteArrayWithInvalidSize_ThrowsArgumentException()
        {
            // Arrange
            var imageData = new byte[] { 1 };

            // Act/Assert
            Assert.Throws<ArgumentException>(() => new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks));
        }

        [TestMethod]
        [DataRow(DepthType.Cv8U, 1)]
        [DataRow(DepthType.Cv8U, 3)]
        [DataRow(DepthType.Cv16U, 1)]
        public void GcBuffer_MatWithValidInputs_IsNotNull(DepthType depthType, int channels)
        {
            // Arrange
            var mat = Mat.Eye(3, 3, depthType, channels);

            // Act
            var buffer = new GcBuffer(mat, (uint)EmguHelper.GetMax(depthType), 0, 0);

            // Assert
            Assert.IsNotNull(buffer);
        }

        [TestMethod]
        [DataRow(2, 2, DepthType.Cv8U, 1)]
        [DataRow(3, 2, DepthType.Cv8U, 3)]
        [DataRow(2, 3, DepthType.Cv16U, 1)]
        public void GcBuffer_MatWithValidInputs_PropertiesAreEqual(int width, int height, DepthType depthType, int channels)
        {
            // Arrange
            var mat = Mat.Eye(height, width, depthType, channels);
            var frameID = Random.Shared.Next();
            var timeStamp = (ulong)DateTime.Now.Ticks;

            // Act
            var buffer = new GcBuffer(mat, (uint)EmguHelper.GetMax(depthType), frameID, timeStamp);

            // Assert
            Assert.AreEqual(buffer.Width, (uint)mat.Width);
            Assert.AreEqual(buffer.Height, (uint)mat.Height);
            Assert.AreEqual(buffer.NumChannels, (uint)mat.NumberOfChannels);
            Assert.AreEqual(buffer.BitDepth, (uint)EmguHelper.GetBitDepth(mat.Depth));
            Assert.AreEqual(buffer.FrameID, frameID);
            Assert.AreEqual(buffer.TimeStamp, timeStamp);
        }

        [TestMethod]
        public void GcBuffer_MatWithValidInputs_AreNotSame()
        {
            // Arrange
            var mat = Mat.Eye(1, 1, DepthType.Cv8U, 1);
            var matData = new byte[mat.Width * mat.Height * mat.ElementSize];
            mat.CopyTo(matData);

            // Act
            var buffer = new GcBuffer(mat, (uint)EmguHelper.GetMax(DepthType.Cv8U), 0, 0);
            mat.SetTo(new MCvScalar(42));

            // Assert
            Assert.AreNotSame(buffer.ImageData, matData); // does not share memory
            Assert.AreNotEqual(buffer.GetPixel(0, 0)[0], mat.GetPixel(0, 0, 0)); // can mutate one without affecting the other
        }

        [TestMethod]
        public void GcBuffer_MatWithEmptyMat_ThrowsArgumentException()
        {
            // Arrange
            var imageData = new Mat();

            // Act
            Assert.Throws<ArgumentException>(() => new GcBuffer(imageData, 255, 42, (ulong)DateTime.Now.Ticks));
        }

        [TestMethod]
        [DataRow(DepthType.Cv8S)]
        [DataRow(DepthType.Cv16S)]
        [DataRow(DepthType.Cv32F)]
        [DataRow(DepthType.Cv32S)]
        [DataRow(DepthType.Cv64F)]
        [DataRow(DepthType.Default)]
        public void GcBuffer_MatWithInvalidDepthType_ThrowsNotSupportedException(DepthType depthType)
        {
            // Arrange
            var mat = Mat.Eye(3, 3, depthType, 1);

            // Act
            Assert.Throws<NotSupportedException>(() => new GcBuffer(mat, (uint)EmguHelper.GetMax(depthType), 42, (ulong)DateTime.Now.Ticks));
        }

        [TestMethod]
        public void CopyConstructor_PropertiesAreEqualButAreNotSameObjects()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);
            var originalBuffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act
            var copiedBuffer = new GcBuffer(originalBuffer);

            // Assert
            Assert.IsTrue(Enumerable.SequenceEqual(copiedBuffer.ImageData, originalBuffer.ImageData)); // element are equal
            Assert.AreEqual(originalBuffer.Width, copiedBuffer.Width);
            Assert.AreEqual(originalBuffer.Height, copiedBuffer.Height);
            Assert.AreEqual(originalBuffer.PixelFormat, copiedBuffer.PixelFormat);
            Assert.AreEqual(originalBuffer.PixelDynamicRangeMax, copiedBuffer.PixelDynamicRangeMax);
            Assert.AreEqual(originalBuffer.PixelDynamicRangeMin, copiedBuffer.PixelDynamicRangeMin);
            Assert.AreEqual(originalBuffer.FrameID, copiedBuffer.FrameID);
            Assert.AreEqual(originalBuffer.TimeStamp, copiedBuffer.TimeStamp);

            Assert.AreNotSame(notExpected: originalBuffer.ImageData, actual: copiedBuffer.ImageData); // does not share memory
        }

        #endregion

        #region MethodTests

        [TestMethod]
        [DataRow(PixelFormat.Mono8)]
        [DataRow(PixelFormat.Mono10)]
        [DataRow(PixelFormat.Mono12)]
        [DataRow(PixelFormat.Mono14)]
        [DataRow(PixelFormat.Mono16)]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void ToMat_IsNotNull(PixelFormat pixelFormat)
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);
            var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);

            // Act
            var mat = buffer.ToMat();

            // Assert
            Assert.IsNotNull(mat);
            Assert.IsFalse(mat.IsEmpty);
        }

        [TestMethod]
        [DataRow(PixelFormat.Mono8, 2, 3)]
        [DataRow(PixelFormat.Mono10, 2, 2)]
        [DataRow(PixelFormat.Mono12, 3, 2)]
        [DataRow(PixelFormat.Mono14, 2, 3)]
        [DataRow(PixelFormat.Mono16, 3, 2)]
        [DataRow(PixelFormat.RGB8, 3, 3)]
        [DataRow(PixelFormat.BGR8, 3, 3)]
        public void ToMat_PropertiesAreEqual(PixelFormat pixelFormat, int width, int height)
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage((uint)width, (uint)height, pixelFormat, TestPattern.GrayHorizontalRamp);
            var buffer = new GcBuffer(imageData, (uint)width, (uint)height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);

            // Act
            var mat = buffer.ToMat();

            // Assert
            Assert.AreEqual(width, mat.Width);
            Assert.AreEqual(height, mat.Height);
            Assert.AreEqual(buffer.NumChannels, (uint)mat.NumberOfChannels);
            Assert.AreEqual(EmguHelper.GetDepthType(pixelFormat), mat.Depth);
        }

        [TestMethod]
        public void ToMat_AreNotSame()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(2, 2, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);
            var buffer = new GcBuffer(imageData, 2, 2, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 0, 0);

            // Act
            var mat = buffer.ToMat();
            var matData = new byte[mat.Width * mat.Height * mat.ElementSize];
            mat.CopyTo(matData);
            buffer.SetPixel(0, 0, 0, 42);

            // Assert
            Assert.AreNotSame(buffer.ImageData, matData); // does not share memory
            Assert.AreNotEqual(buffer.GetPixel(0, 0)[0], mat.GetPixel(0, 0, 0)); // can mutate one without affecting the other
        }

        [TestMethod]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void ToMat_Red_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
        {
            // Arrange
            uint width = 3; uint height = 3;
            var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.Red);
            var buffer = new GcBuffer(imageData, width, height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);
            var mat = buffer.ToMat();

            // Act/Assert
            for (int col = 0; col < mat.Width; col++)
                for (int row = 0; row < mat.Height; row++)
                {
                    if (pixelFormat == PixelFormat.RGB8)
                    {
                        Assert.AreEqual(255, mat.GetPixel(row, col, 0)); // R
                        Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                        Assert.AreEqual(0, mat.GetPixel(row, col, 2)); // B
                    }
                    else if (pixelFormat == PixelFormat.BGR8)
                    {
                        Assert.AreEqual(0, mat.GetPixel(row, col, 0)); // B
                        Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                        Assert.AreEqual(255, mat.GetPixel(row, col, 2)); // R
                    }
                }
        }

        [TestMethod]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void ToMat_Green_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
        {
            // Arrange
            uint width = 3; uint height = 3;
            var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.Green);
            var buffer = new GcBuffer(imageData, width, height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);
            var mat = buffer.ToMat();

            // Act/Assert
            for (int col = 0; col < mat.Width; col++)
                for (int row = 0; row < mat.Height; row++)
                {
                    Assert.AreEqual(0, mat.GetPixel(row, col, 0)); // R
                    Assert.AreEqual(255, mat.GetPixel(row, col, 1)); // G
                    Assert.AreEqual(0, mat.GetPixel(row, col, 2)); // B
                }
        }

        [TestMethod]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void GetPixel_Blue_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
        {
            // Arrange
            uint width = 3; uint height = 3;
            var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.Blue);
            var buffer = new GcBuffer(imageData, width, height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);
            var mat = buffer.ToMat();

            // Act/Assert
            for (int col = 0; col < mat.Width; col++)
                for (int row = 0; row < mat.Height; row++)
                {
                    if (pixelFormat == PixelFormat.RGB8)
                    {
                        Assert.AreEqual(0, mat.GetPixel(row, col, 0)); // R
                        Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                        Assert.AreEqual(255, mat.GetPixel(row, col, 2)); // B
                    }
                    else if (pixelFormat == PixelFormat.BGR8)
                    {
                        Assert.AreEqual(255, mat.GetPixel(row, col, 0)); // B
                        Assert.AreEqual(0, mat.GetPixel(row, col, 1)); // G
                        Assert.AreEqual(0, mat.GetPixel(row, col, 2)); // R
                    }
                }
        }

        [TestMethod]
        [DataRow(PixelFormat.Mono8)]
        [DataRow(PixelFormat.Mono10)]
        [DataRow(PixelFormat.Mono12)]
        [DataRow(PixelFormat.Mono14)]
        [DataRow(PixelFormat.Mono16)]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void GetPixel_ForSpecificChannel_ValidatePixelValues(PixelFormat pixelFormat)
        {
            // Arrange
            uint width = 3; uint height = 3;
            var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.White);
            var buffer = new GcBuffer(imageData, width, height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            for (int col = 0; col < buffer.Width; col++)
                for (int row = 0; row < buffer.Height; row++)
                    for (int ch = 0; ch < buffer.NumChannels; ch++)
                        Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), buffer.GetPixel((uint)row, (uint)col, (uint)ch));
        }

        [TestMethod]
        [DataRow(PixelFormat.Mono8)]
        [DataRow(PixelFormat.Mono10)]
        [DataRow(PixelFormat.Mono12)]
        [DataRow(PixelFormat.Mono14)]
        [DataRow(PixelFormat.Mono16)]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void GetPixel_ForAllChannels_ValidatePixelValues(PixelFormat pixelFormat)
        {
            // Arrange
            uint width = 3; uint height = 3;
            var imageData = TestPatternGenerator.CreateImage(width, height, pixelFormat, TestPattern.White);
            var buffer = new GcBuffer(imageData, width, height, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            for (int col = 0; col < buffer.Width; col++)
                for (int row = 0; row < buffer.Height; row++)
                    for (int ch = 0; ch < buffer.NumChannels; ch++)
                    {
                        var pixelValues = buffer.GetPixel((uint)row, (uint)col);
                        foreach (var pixelValue in pixelValues)
                            Assert.AreEqual(GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), pixelValue);
                    }
        }

        [TestMethod]
        public void GetPixel_RowOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(3, 2));
            Assert.AreEqual("row", ex.ParamName);
            ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(3, 2, 0));
            Assert.AreEqual("row", ex.ParamName);
        }

        [TestMethod]
        public void GetPixel_ColOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(2, 3));
            Assert.AreEqual("col", ex.ParamName);
            ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(2, 3, 0));
            Assert.AreEqual("col", ex.ParamName);
        }

        [TestMethod]
        public void GetPixel_ChannelOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(2, 2, 1));
            Assert.AreEqual("channel", ex.ParamName);
        }

        [TestMethod]
        [DataRow(PixelFormat.Mono8)]
        [DataRow(PixelFormat.Mono10)]
        [DataRow(PixelFormat.Mono12)]
        [DataRow(PixelFormat.Mono14)]
        [DataRow(PixelFormat.Mono16)]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void SetPixel_ForSpecificChannel_ValidateChangedPixelValues(PixelFormat pixelFormat)
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);
            var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);

            // Act
            int counter = 0;
            for (int col = 0; col < buffer.Width; col++)
                for (int row = 0; row < buffer.Height; row++)
                    for (int ch = 0; ch < buffer.NumChannels; ch++)
                    {
                        buffer.SetPixel((uint)row, (uint)col, (uint)ch, counter++);
                    }

            // Assert
            counter = 0;
            for (int col = 0; col < buffer.Width; col++)
                for (int row = 0; row < buffer.Height; row++)
                    for (int ch = 0; ch < buffer.NumChannels; ch++)
                    {
                        Assert.AreEqual(buffer.GetPixel((uint)row, (uint)col, (uint)ch), counter++);
                    }
        }

        [TestMethod]
        [DataRow(PixelFormat.Mono8)]
        [DataRow(PixelFormat.Mono10)]
        [DataRow(PixelFormat.Mono12)]
        [DataRow(PixelFormat.Mono14)]
        [DataRow(PixelFormat.Mono16)]
        [DataRow(PixelFormat.RGB8)]
        [DataRow(PixelFormat.BGR8)]
        public void SetPixel_ForAllChannels_ValidateChangedPixelValues(PixelFormat pixelFormat)
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, pixelFormat, TestPattern.GrayHorizontalRamp);
            var buffer = new GcBuffer(imageData, 3, 3, pixelFormat, GenICamHelper.GetPixelDynamicRangeMax(pixelFormat), 42, (ulong)DateTime.Now.Ticks);

            // Act
            for (int col = 0; col < buffer.Width; col++)
                for (int row = 0; row < buffer.Height; row++)
                    for (int ch = 0; ch < buffer.NumChannels; ch++)
                    {
                        var pixelValues = new double[buffer.NumChannels];
                        for (int i = 0; i < buffer.NumChannels; i++)
                            pixelValues[i] = GenICamHelper.GetPixelDynamicRangeMax(pixelFormat);

                        buffer.SetPixel((uint)row, (uint)col, pixelValues);
                    }

            // Assert
            for (int col = 0; col < buffer.Width; col++)
                for (int row = 0; row < buffer.Height; row++)
                    for (int ch = 0; ch < buffer.NumChannels; ch++)
                    {
                        Assert.AreEqual(buffer.GetPixel((uint)row, (uint)col, (uint)ch), GenICamHelper.GetPixelDynamicRangeMax(pixelFormat));
                    }
        }

        [TestMethod]
        public void SetPixel_RowOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(3, 2, 0, 33));
            Assert.AreEqual("row", ex.ParamName);
            ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(3, 2, [33]));
            Assert.AreEqual("row", ex.ParamName);
        }

        [TestMethod]
        public void SetPixel_ColOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 3, 0, 33));
            Assert.AreEqual("col", ex.ParamName);
            ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 3, [33]));
            Assert.AreEqual("col", ex.ParamName);
        }

        [TestMethod]
        public void SetPixel_ChannelOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, 1, 33));
            Assert.AreEqual("channel", ex.ParamName);
            ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, [33, 33, 33]));
            Assert.AreEqual("pixelValues", ex.ParamName);
        }

        [TestMethod]
        public void SetPixel_PixelValueOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage(3, 3, PixelFormat.Mono8, TestPattern.White);
            var buffer = new GcBuffer(imageData, 3, 3, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act/Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, 0, 256));
            Assert.AreEqual("pixelValue", ex.ParamName);
            ex = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.SetPixel(2, 2, [33, 2, -5]));
            Assert.AreEqual("pixelValues", ex.ParamName);
        }

        [TestMethod]
        [DataRow(2, 2)]
        [DataRow(3, 2)]
        [DataRow(2, 3)]
        [DataRow(3, 3)]
        [DataRow(4, 4)]
        [DataRow(100, 100)]
        public void GetSize_SizeIsValid(int width, int height)
        {
            // Arrange
            var imageData = TestPatternGenerator.CreateImage((uint)width, (uint)height, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);
            var buffer = new GcBuffer(imageData, (uint)width, (uint)height, PixelFormat.Mono8, GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), 42, (ulong)DateTime.Now.Ticks);

            // Act
            var size = buffer.GetSize();

            // Assert
            Assert.AreEqual(height, size.Height);
            Assert.AreEqual(width, size.Width);
        }

        #endregion
    }
}