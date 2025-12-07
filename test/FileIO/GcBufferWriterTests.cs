using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class GcBufferWriterTests
    {
        #region Fields

        private readonly string _path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"output.bin");
        private GcBufferWriter _writer;

        private event EventHandler<BufferTransferredEventArgs> BufferTransferred;

        #endregion

        #region TestConfiguration

        [TestInitialize]
        public void TestInitialize()
        {
            _writer = new GcBufferWriter(_path);

            BufferTransferred += _writer.OnBufferTransferred;
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            BufferTransferred -= _writer.OnBufferTransferred;

            if (_writer.IsWriting)
                await _writer?.StopAsync();
            _writer?.Dispose();
            _writer = null;

            if (File.Exists(_path))
                File.Delete(_path);
        }

        #endregion

        #region Tests

        #region Constructor

        [TestMethod]
        public void GcBufferWriter_ValidateInitialization()
        {
            // Assert
            Assert.AreEqual<uint>(0, GcBufferWriter.FileHeaderSize);
            Assert.AreEqual<uint>(32, GcBufferWriter.ImageHeaderSize);

            Assert.IsNotNull(_writer);
            Assert.IsFalse(_writer.IsDisposed);
            Assert.IsFalse(_writer.IsWriting);
            Assert.AreEqual(0, _writer.FileSize);
            Assert.AreEqual(_path, _writer.FilePath);
            Assert.AreEqual(0, _writer.BuffersQueued);
            Assert.AreEqual(System.Threading.ThreadPriority.BelowNormal, _writer.ThreadPriority);
        }

        #endregion

        #region Start

        [TestMethod]
        public void Start_IsNotWriting_IsWriting()
        {
            // Act
            _writer.Start();

            // Assert
            Assert.IsTrue(_writer.IsWriting);
            Assert.AreEqual(GcBufferWriter.FileHeaderSize, _writer.FileSize);
        }

        [TestMethod]
        public void Start_IsWriting_ThrowsInvalidOperationException()
        {
            // Arrange
            _writer.Start();

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => _writer.Start());
        }

        [TestMethod]
        public void Start_IsDisposed_ThrowsObjectDisposedException()
        {
            // Arrange
            _writer.Dispose();

            // Act/Assert
            Assert.Throws<ObjectDisposedException>(_writer.Start);
        }

        #endregion

        #region StopAsync

        [TestMethod]
        public async Task StopAsync_IsNotWriting_IsNotWriting()
        {
            // Act
            await _writer.StopAsync();

            // Assert
            Assert.IsFalse(_writer.IsWriting);
        }

        [TestMethod]
        public async Task StopAsync_IsWriting_IsNotWriting()
        {
            // Arrange
            _writer.Start();

            // Act
            await _writer.StopAsync();

            // Act/Assert
            Assert.IsFalse(_writer.IsWriting);
        }

        [TestMethod]
        public async Task StopAsync_TransferSingleBuffer_IsWritingIsFalse()
        {
            // Arrange
            _writer.Start();
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));

            // Act
            await _writer.StopAsync();

            // Assert
            Assert.IsFalse(_writer.IsWriting);
        }

        [TestMethod]
        public async Task StopAsync_TransferSingleBuffer_BuffersAccounted()
        {
            // Arrange
            _writer.Start();
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));

            // Act
            await _writer.StopAsync();

            // Assert
            Assert.AreEqual(0, _writer.BuffersQueued);
            Assert.AreEqual(1, _writer.BuffersWritten);
        }

        [TestMethod]
        public async Task StopAsync_TransferSingleBuffer_FileSizeIsCorrect()
        {
            // Arrange
            _writer.Start();
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));

            // Act
            await _writer.StopAsync();
            _writer.Dispose();

            // Assert
            Assert.AreEqual(new FileInfo(_path).Length, _writer.FileSize);
        }

        [TestMethod]
        public async Task StopAsync_TransferMultipleBuffers_WritingIsFalse()
        {
            // Arrange
            int numBuffers = 30;
            _writer.Start();
            for (int i = 0; i < numBuffers; i++)
                BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

            // Act
            await _writer.StopAsync();

            // Assert
            Assert.IsFalse(_writer.IsWriting);
        }

        [TestMethod]
        public async Task StopAsync_TransferMultipleBuffers_BuffersAccounted()
        {
            // Arrange
            int numBuffers = 30;
            _writer.Start();
            for (int i = 0; i < numBuffers; i++)
                BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

            // Act
            await _writer.StopAsync();

            // Assert
            Assert.AreEqual(0, _writer.BuffersQueued);
            Assert.AreEqual(numBuffers, _writer.BuffersWritten);
        }

        [TestMethod]
        public async Task StopAsync_TransferMultipleBuffers_FileSizeIsCorrect()
        {
            // Arrange
            int numBuffers = 30;
            _writer.Start();

            for (int i = 0; i < numBuffers; i++)
                BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

            // Act
            await _writer.StopAsync();
            _writer.Dispose();

            // Assert
            Assert.AreEqual(new FileInfo(_path).Length, _writer.FileSize);
        }

        [TestMethod]
        public async Task StopAsyncWithDiscard_TransferMultipleBuffers_WritingIsFalse()
        {
            // Arrange
            int numBuffers = 30;
            _writer.Start();
            for (int i = 0; i < numBuffers; i++)
                BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

            // Act
            await _writer.StopAsync(true);

            // Assert
            Assert.IsFalse(_writer.IsWriting);
        }

        [TestMethod]
        public async Task StopAsyncWithDiscard_TransferMultipleBuffers_BuffersAccounted()
        {
            // Arrange
            int numBuffers = 30;
            _writer.Start();
            for (int i = 0; i < numBuffers; i++)
                BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

            // Act
            await _writer.StopAsync(true);

            // Assert
            Assert.AreEqual(0, _writer.BuffersQueued);
            Assert.IsLessThanOrEqualTo(numBuffers, _writer.BuffersWritten);
        }

        [TestMethod]
        public async Task StopAsyncWithDiscard_TransferMultipleBuffers_FileSizeIsCorrect()
        {
            // Arrange
            int numBuffers = 30;
            _writer.Start();
            for (int i = 0; i < numBuffers; i++)
                BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

            // Act
            await _writer.StopAsync(true);

            // Act
            _writer.Dispose();

            // Assert
            Assert.AreEqual(new FileInfo(_path).Length, _writer.FileSize);
        }

        #endregion

        #region IDisposable

        [TestMethod]
        public void Dispose_IsNotWriting_IsDisposed()
        {
            // Act
            _writer.Dispose();

            // Assert
            Assert.IsTrue(_writer.IsDisposed);
            Assert.AreEqual(0, _writer.BuffersQueued);
        }

        [TestMethod]
        public async Task Dispose_PropertiesAreStillValid()
        {
            // Arrange
            int numBuffers = 30;
            _writer.Start();
            for (int i = 0; i < numBuffers; i++)
                BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));
            await _writer.StopAsync();

            // Act
            _writer.Dispose();

            // Assert
            Assert.IsNotNull(_writer);
            Assert.IsTrue(_writer.IsDisposed);
            Assert.IsFalse(_writer.IsWriting);
            Assert.IsGreaterThan(0, _writer.FileSize);
            Assert.AreEqual(_path, _writer.FilePath);
            Assert.AreEqual(0, _writer.BuffersQueued);
            Assert.AreEqual(System.Threading.ThreadPriority.BelowNormal, _writer.ThreadPriority);
        }

        #endregion

        #endregion

        #region Private methods

        private static GcBuffer GetBuffer(int n = 42)
        {
            byte[] data = ImagePatternGenerator.CreateImage(width: 3,
                                                            height: 3,
                                                            pixelFormat: PixelFormat.Mono8,
                                                            testPattern: TestPattern.GrayHorizontalRamp,
                                                            frameNumber: 42);

            return new GcBuffer(imageData: data, width: 3, height: 3, pixelFormat: PixelFormat.Mono8, pixelDynamicRangeMax: GenICamConverter.GetDynamicRangeMax(PixelFormat.Mono8), frameID: n, timeStamp: (ulong)DateTime.Now.Ticks);
        }

        #endregion
    }
}