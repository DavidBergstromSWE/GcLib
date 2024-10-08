using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using GcLib.Utilities.Imaging;
using System.Threading.Tasks;
using System.Threading;
using Moq;

namespace GcLib.UnitTests
{
    [TestClass]
    public class GcBufferReaderTests
    {
        #region Fields

        private readonly string _pathToValidFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, @"Resources\test_320x240_Mono8_10frames.bin");
        private readonly string _pathToInvalidFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, @"Resources\fake.bin");
        private GcBufferReader _reader;

        #endregion

        #region TestConfiguration

        [TestCleanup]
        public void TestCleanup()
        {
            _reader?.Dispose();
            _reader = null;
        }

        #endregion

        #region Tests

        #region Constructor

        [TestMethod]
        public void GcBufferReader_ValidateInitialization()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);

            // Assert
            Assert.IsNotNull(_reader);

            Assert.IsTrue(_reader.FilePath == _pathToValidFile);
            Assert.IsTrue(_reader.FrameCount == 10);
            Assert.IsTrue(_reader.PayloadSize == 320 * 240 * GenICamConverter.GetBitsPerPixel(PixelFormat.Mono8) / 8);
            Assert.IsTrue(_reader.FileSize == 768320);
            Assert.IsTrue(_reader.FrameIndex == 0);
            Assert.IsTrue(_reader.FrameRate == 0);
            Assert.IsFalse(_reader.IsDisposed);
            Assert.IsFalse(_reader.IsOpen);
        }

        [TestMethod]
        public void GcBufferReader_InvalidFile_ThrowsIOException()
        {
            // Act/Assert
            Assert.ThrowsException<IOException>(() => new GcBufferReader(_pathToInvalidFile));
        }

        #endregion

        #region OpenAsync

        [TestMethod]
        public async Task OpenAsync_ValidateProperties()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);

            // Act
            await _reader.OpenAsync();

            // Assert
            Assert.IsTrue(_reader.IsOpen);
            Assert.IsTrue(_reader.FrameCount == 10);
            Assert.IsTrue(_reader.FrameRate >= 34.8 && _reader.FrameRate <= 34.9);
            Assert.IsTrue(_reader.FrameIndex == 0);
        }

        [TestMethod]
        public async Task OpenAsync_Cancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            var tokenSource = new CancellationTokenSource(0);

            // Act/Assert
            await Assert.ThrowsExceptionAsync<OperationCanceledException>(() => _reader.OpenAsync(token: tokenSource.Token));
            Assert.IsTrue(tokenSource.Token.IsCancellationRequested);

            tokenSource?.Dispose();
        }

        [TestMethod]
        public async Task OpenAsync_Progress_ReportsProgress()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            var actualReportValue = 0.0;

            var mockProgress = new Mock<IProgress<double>>();
            mockProgress.Setup(p => p.Report(It.IsAny<double>())).Callback((double d) => { actualReportValue = d; Assert.IsTrue(d >= 0 && d <= 1); });

            var expectedReportValue = 1.0;

            // Act
            await _reader.OpenAsync(progress: mockProgress.Object, CancellationToken.None);

            // Assert
            Assert.AreEqual(expectedReportValue, actualReportValue);
        }

        #endregion

        #region ReadImage

        [TestMethod]
        public async Task ReadNextImage_SingleImageFromStart_BufferIsNotNull()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();

            // Act
            var isRead = _reader.ReadImage(out GcBuffer buffer);

            // Assert
            Assert.IsTrue(isRead);
            Assert.IsNotNull(buffer);
        }

        [TestMethod]
        public async Task ReadNextImage_AllImagesFromStart__BufferCountAsExpected()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();
            int expectedFrameCount = (int)_reader.FrameCount;
            int actualFrameCount = 0;

            // Act/Assert
            while (_reader.ReadImage(out GcBuffer buffer))
            {
                Assert.IsNotNull(buffer);
                actualFrameCount++;
            }

            // Assert
            Assert.IsTrue(actualFrameCount == expectedFrameCount);
        }

        [TestMethod]
        public async Task ReadNextImage_SingleImageAtEOF_BufferIsNull()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();
            while (_reader.ReadImage(out _)) { }

            // Act
            bool isRead = _reader.ReadImage(out GcBuffer buffer);

            // Assert
            Assert.IsFalse(isRead);
            Assert.IsNull(buffer);
        }

        [TestMethod]
        public async Task ReadIndexedImage_SingleImageAtStart_BufferIsNotNull()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();

            // Act
            bool isRead = _reader.ReadImage(out GcBuffer buffer, 0);

            // Assert
            Assert.IsTrue(isRead);
            Assert.IsNotNull(buffer);
        }

        [TestMethod]
        public async Task ReadIndexedImage_SingleImageInsideRange_BufferIsNotNull()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();

            // Act
            bool isRead = _reader.ReadImage(out GcBuffer buffer, 5);

            // Assert
            Assert.IsTrue(isRead);
            Assert.IsNotNull(buffer);
        }

        [TestMethod]
        public async Task ReadIndexedImage_SingleImageAtEnd_BufferIsNotNull()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();

            // Act
            bool isRead = _reader.ReadImage(out GcBuffer buffer, _reader.FrameCount - 1);

            // Assert
            Assert.IsTrue(isRead);
            Assert.IsNotNull(buffer);
        }

        [TestMethod]
        public async Task ReadIndexedImage_SingleImageOutofRange_BufferIsNull()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();

            // Act
            bool isRead = _reader.ReadImage(out GcBuffer buffer, _reader.FrameCount);

            // Assert
            Assert.IsFalse(isRead);
            Assert.IsNull(buffer);
        }

        [TestMethod]
        public void ReadImage_IsDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            _reader.Dispose();

            // Act/Assert
            Assert.ThrowsException<ObjectDisposedException>(() => _reader.ReadImage(out GcBuffer buffer, 0));
            Assert.ThrowsException<ObjectDisposedException>(() => _reader.ReadImage(out GcBuffer buffer));
        }

        [TestMethod]
        public void ReadImage_IsNotOpen_ThrowsInvalidOperationException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);

            // Act/Assert
            Assert.ThrowsException<InvalidOperationException>(() => _reader.ReadImage(out GcBuffer buffer, 0));
            Assert.ThrowsException<InvalidOperationException>(() => _reader.ReadImage(out GcBuffer buffer));
        }

        #endregion

        #region GetTimeStamp

        [TestMethod]
        public async Task GetTimeStamp_WithinRange_ReturnValueIsValid()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();
            _reader.ReadImage(out GcBuffer buffer, 4);
            var expectedTime = buffer.TimeStamp;

            // Act
            var actualTime = _reader.GetTimeStamp(4);

            // Assert
            Assert.IsTrue(expectedTime == actualTime);
        }

        [TestMethod]
        public async Task GetTimeStamp_OutofRange_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync();

            // Act/Assert
            Assert.ThrowsException<IndexOutOfRangeException>(() => _reader.GetTimeStamp(_reader.FrameCount));
        }

        [TestMethod]
        public void GetTimeStamp_IsDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            _reader.Dispose();

            // Act/Assert
            Assert.ThrowsException<ObjectDisposedException>(() => _reader.GetTimeStamp(0));
        }

        [TestMethod]
        public void GetTimeStamp_IsNotOpen_ThrowsInvalidOperationException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);

            // Act/Assert
            Assert.ThrowsException<InvalidOperationException>(() => _reader.GetTimeStamp(0));
        }

        #endregion

        #region IDisposable

        [TestMethod]
        public void Dispose_IsDisposed()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            _reader.Dispose();

            // Assert
            Assert.IsTrue(_reader.IsDisposed);
            Assert.IsFalse(_reader.IsOpen);
        }

        #endregion

        #endregion
    }
}