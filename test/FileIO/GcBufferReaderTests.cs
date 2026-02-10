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
    [DoNotParallelize]
    public class GcBufferReaderTests
    {
        #region Fields

        private readonly string _pathToValidFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, @"Resources\test_320x240_Mono8_10frames.bin");
        private readonly string _pathToInvalidFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, @"Resources\fake.bin");
        private GcBufferReader _reader;

        #endregion

        #region Properties

        public TestContext TestContext { get; set; }

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

            Assert.AreEqual(_pathToValidFile, _reader.FilePath);
            Assert.AreEqual<ulong>(10, _reader.FrameCount);
            Assert.AreEqual(320 * 240 * GenICamHelper.GetBitsPerPixel(PixelFormat.Mono8) / 8, _reader.PayloadSize);
            Assert.AreEqual<ulong>(768320, _reader.FileSize);
            Assert.AreEqual<ulong>(0, _reader.FrameIndex);
            Assert.AreEqual<double>(0, _reader.FrameRate);
            Assert.IsFalse(_reader.IsDisposed);
            Assert.IsFalse(_reader.IsOpen);
        }

        [TestMethod]
        public void GcBufferReader_InvalidFile_ThrowsIOException()
        {
            // Act/Assert
            Assert.Throws<IOException>(() => new GcBufferReader(_pathToInvalidFile));
        }

        #endregion

        #region OpenAsync

        [TestMethod]
        public async Task OpenAsync_ValidateProperties()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);

            // Act
            await _reader.OpenAsync(token: TestContext.CancellationToken);

            // Assert
            Assert.IsTrue(_reader.IsOpen);
            Assert.AreEqual<ulong>(10, _reader.FrameCount);
            Assert.IsTrue(_reader.FrameRate >= 34.8 && _reader.FrameRate <= 34.9);
            Assert.AreEqual<ulong>(0, _reader.FrameIndex);
        }

        [TestMethod]
        public async Task OpenAsync_Cancellation_ThrowsOperationCanceledException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            var tokenSource = new CancellationTokenSource(0);

            // Act/Assert
            await Assert.ThrowsAsync<OperationCanceledException>(() => _reader.OpenAsync(token: tokenSource.Token));
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
            await _reader.OpenAsync(token: TestContext.CancellationToken);

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
            await _reader.OpenAsync(token: TestContext.CancellationToken);
            int expectedFrameCount = (int)_reader.FrameCount;
            int actualFrameCount = 0;

            // Act/Assert
            while (_reader.ReadImage(out GcBuffer buffer))
            {
                Assert.IsNotNull(buffer);
                actualFrameCount++;
            }

            // Assert
            Assert.AreEqual(expectedFrameCount, actualFrameCount);
        }

        [TestMethod]
        public async Task ReadNextImage_SingleImageAtEOF_BufferIsNull()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync(token: TestContext.CancellationToken);
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
            await _reader.OpenAsync(token: TestContext.CancellationToken);

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
            await _reader.OpenAsync(token: TestContext.CancellationToken);

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
            await _reader.OpenAsync(token: TestContext.CancellationToken);

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
            await _reader.OpenAsync(token: TestContext.CancellationToken);

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
            Assert.Throws<ObjectDisposedException>(() => _reader.ReadImage(out GcBuffer buffer, 0));
            Assert.Throws<ObjectDisposedException>(() => _reader.ReadImage(out GcBuffer buffer));
        }

        [TestMethod]
        public void ReadImage_IsNotOpen_ThrowsInvalidOperationException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => _reader.ReadImage(out GcBuffer buffer, 0));
            Assert.Throws<InvalidOperationException>(() => _reader.ReadImage(out GcBuffer buffer));
        }

        #endregion

        #region GetTimeStamp

        [TestMethod]
        public async Task GetTimeStamp_WithinRange_ReturnValueIsValid()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync(token: TestContext.CancellationToken);
            _reader.ReadImage(out GcBuffer buffer, 4);
            var expectedTime = buffer.TimeStamp;

            // Act
            var actualTime = _reader.GetTimeStamp(4);

            // Assert
            Assert.AreEqual(expectedTime, actualTime);
        }

        [TestMethod]
        public async Task GetTimeStamp_OutofRange_ThrowsIndexOutOfRangeException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            await _reader.OpenAsync(token: TestContext.CancellationToken);

            // Act/Assert
            Assert.Throws<IndexOutOfRangeException>(() => _reader.GetTimeStamp(_reader.FrameCount));
        }

        [TestMethod]
        public void GetTimeStamp_IsDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);
            _reader.Dispose();

            // Act/Assert
            Assert.Throws<ObjectDisposedException>(() => _reader.GetTimeStamp(0));
        }

        [TestMethod]
        public void GetTimeStamp_IsNotOpen_ThrowsInvalidOperationException()
        {
            // Arrange
            _reader = new GcBufferReader(_pathToValidFile);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => _reader.GetTimeStamp(0));
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