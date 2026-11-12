using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GcLib.Utilities.FileIO;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests.Utilities;

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

    [TestMethod]
    [DataRow(0, false)]
    [DataRow(31, false)]
    [DataRow(32, true)]
    [DataRow(33, false)]
    public void GcBufferReader_ConstructorFails_ReleasesFile(int fileSize, bool invalidPixelFormat)
    {
        string path = Path.GetTempFileName();
        try
        {
            var data = new byte[fileSize];
            if (fileSize >= GcBufferWriter.ImageHeaderSize)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(2u), 0, data, 16, sizeof(uint));
                Buffer.BlockCopy(BitConverter.GetBytes(1u), 0, data, 20, sizeof(uint));
                var pixelFormat = invalidPixelFormat ? PixelFormat.InvalidPixelFormat : PixelFormat.Mono8;
                Buffer.BlockCopy(BitConverter.GetBytes((int)pixelFormat), 0, data, 24, sizeof(int));
            }
            File.WriteAllBytes(path, data);

            Assert.Throws<IOException>(() => _reader = new GcBufferReader(path));

            using var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            Assert.AreEqual((long)fileSize, file.Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    #endregion

    #region Truncated files

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(32)] // Header of the next frame is complete, but its image data is missing.
    public async Task OpenAsync_FileEndsWithPartialFrame_ReadsCompleteFrames(int extraBytes)
    {
        string path = CreateTruncatedFile(frameCount: 5, extraBytes);
        try
        {
            using var reader = new GcBufferReader(path);
            Assert.IsTrue(reader.IsTruncated);
            Assert.AreEqual((ulong)extraBytes, reader.TruncatedBytes);
            Assert.AreEqual<ulong>(5, reader.FrameCount);

            await reader.OpenAsync(token: TestContext.CancellationToken);

            Assert.IsTrue(reader.IsOpen);
            Assert.AreEqual<ulong>(5, reader.FrameCount);

            for (ulong i = 0; i < 5; i++)
            {
                Assert.IsTrue(reader.ReadImage(out GcBuffer buffer, i));
                Assert.AreEqual((long)i, buffer.FrameID);
            }

            Assert.IsFalse(reader.ReadImage(out _, 5));

            // Sequential reading stops after the last complete frame.
            reader.FrameIndex = 4;
            Assert.IsTrue(reader.ReadImage(out _));
            Assert.IsFalse(reader.ReadImage(out GcBuffer next));
            Assert.IsNull(next);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task OpenAsync_CompleteFile_IsNotTruncated()
    {
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        Assert.IsFalse(_reader.IsTruncated);
        Assert.AreEqual<ulong>(0, _reader.TruncatedBytes);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(3)]
    public void GcBufferReader_FirstFrameIncomplete_ThrowsIOExceptionAndReleasesFile(int imageBytesWritten)
    {
        // A 32-byte header followed by fewer image bytes than the header specifies (2x2 Mono8 requires 4 bytes).
        string path = Path.GetTempFileName();
        try
        {
            byte[] data = new byte[32 + imageBytesWritten];
            Buffer.BlockCopy(BitConverter.GetBytes(2u), 0, data, 16, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(2u), 0, data, 20, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes((int)PixelFormat.Mono8), 0, data, 24, sizeof(int));
            File.WriteAllBytes(path, data);

            Assert.Throws<IOException>(() => _reader = new GcBufferReader(path));

            using var file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTruncatedFile(int frameCount, int extraBytes)
    {
        // 1x1 Mono8 frames: 32-byte header + 1 byte of image data = 33 bytes per frame.
        string path = Path.GetTempFileName();
        using var stream = new MemoryStream();
        for (int i = 0; i < frameCount; i++)
        {
            byte[] frame = new byte[33];
            Buffer.BlockCopy(BitConverter.GetBytes((long)i), 0, frame, 0, sizeof(long));
            Buffer.BlockCopy(BitConverter.GetBytes((ulong)(TimeSpan.TicksPerDay + i * 1000000L)), 0, frame, 8, sizeof(ulong));
            Buffer.BlockCopy(BitConverter.GetBytes(1u), 0, frame, 16, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes(1u), 0, frame, 20, sizeof(uint));
            Buffer.BlockCopy(BitConverter.GetBytes((int)PixelFormat.Mono8), 0, frame, 24, sizeof(int));
            stream.Write(frame);
        }

        // Partial next frame (first bytes of the header of a frame that was never completed).
        byte[] partial = new byte[extraBytes];
        stream.Write(partial);

        File.WriteAllBytes(path, stream.ToArray());
        return path;
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
        double expectedFrameRate = 9.0 * TimeSpan.TicksPerSecond / (_reader.GetTimeStamp(9) - _reader.GetTimeStamp(0));
        Assert.AreEqual(expectedFrameRate, _reader.FrameRate, 0.000001);
        Assert.AreEqual<ulong>(0, _reader.FrameIndex);
    }

    [TestMethod]
    [DataRow(1, 10000000L, 0.0)]
    [DataRow(2, 10000000L, 1.0)]
    [DataRow(11, 1000000L, 10.0)]
    [DataRow(2, 0L, 0.0)]
    [DataRow(2, -10000000L, 0.0)]
    public async Task OpenAsync_KnownTimestamps_CalculatesFrameRate(int frameCount, long intervalTicks, double expectedFrameRate)
    {
        string path = Path.GetTempFileName();
        try
        {
            using (var writer = new GcBufferWriter(path))
            {
                writer.Start();
                for (int i = 0; i < frameCount; i++)
                {
                    var buffer = new GcBuffer(new byte[1], 1, 1, PixelFormat.Mono8, i,
                        (ulong)(TimeSpan.TicksPerDay + i * intervalTicks));
                    writer.OnBufferTransferred(this, new BufferTransferredEventArgs(buffer));
                }
                await writer.StopAsync();
            }

            using var reader = new GcBufferReader(path);
            await reader.OpenAsync(token: TestContext.CancellationToken);

            Assert.AreEqual((ulong)frameCount, reader.FrameCount);
            Assert.AreEqual(expectedFrameRate, reader.FrameRate, 0.000001);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public async Task OpenAsync_CalledTwice_ReopensWithSameFrames()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);
        ulong lastTimeStamp = _reader.GetTimeStamp(9);

        // Act
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Assert
        Assert.IsTrue(_reader.IsOpen);
        Assert.AreEqual<ulong>(10, _reader.FrameCount);
        Assert.AreEqual(lastTimeStamp, _reader.GetTimeStamp(9));
    }

    [TestMethod]
    public async Task OpenAsync_AfterCancellation_CanBeOpened()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        using var tokenSource = new CancellationTokenSource(0);
        await Assert.ThrowsAsync<OperationCanceledException>(() => _reader.OpenAsync(token: tokenSource.Token));

        // Act
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Assert
        Assert.IsTrue(_reader.IsOpen);
        Assert.AreEqual<ulong>(10, _reader.FrameCount);
    }

    [TestMethod]
    public async Task OpenAsync_IsDisposed_ThrowsObjectDisposedException()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        _reader.Dispose();

        // Act/Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => _reader.OpenAsync(token: TestContext.CancellationToken));
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

    #region Constructor (file handling)

    [TestMethod]
    public void GcBufferReader_FileDoesNotExist_ThrowsFileNotFoundException()
    {
        string path = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.bin");

        Assert.Throws<FileNotFoundException>(() => _reader = new GcBufferReader(path));
    }

    [TestMethod]
    public void GcBufferReader_FileIsLockedUntilDisposed()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);

        // Act/Assert
        Assert.Throws<IOException>(() => { using var file = new FileStream(_pathToValidFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); });

        _reader.Dispose();

        using var released = new FileStream(_pathToValidFile, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    #endregion

    #region OpenAsync (more)

    [TestMethod]
    public async Task OpenAsync_Cancellation_ReaderIsNotOpen()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        using var tokenSource = new CancellationTokenSource(0);

        // Act
        await Assert.ThrowsAsync<OperationCanceledException>(() => _reader.OpenAsync(token: tokenSource.Token));

        // Assert
        Assert.IsFalse(_reader.IsOpen);
        Assert.Throws<InvalidOperationException>(() => _reader.ReadImage(out _));
    }

    [TestMethod]
    public async Task OpenAsync_Progress_IsMonotonicAndEndsAtOne()
    {
        // Arrange (progress is throttled, so the file needs enough frames to report more than once).
        string path = Path.GetTempFileName();
        try
        {
            using (var writer = new GcBufferWriter(path))
            {
                writer.Start();
                for (int i = 0; i < 60; i++)
                    writer.OnBufferTransferred(this, new BufferTransferredEventArgs(new GcBuffer(new byte[1], 1, 1, PixelFormat.Mono8, i, (ulong)(TimeSpan.TicksPerDay + i))));
                await writer.StopAsync();
            }

            using var reader = new GcBufferReader(path);
            var reports = new System.Collections.Generic.List<double>();

            // Act
            await reader.OpenAsync(new SynchronousProgress(reports.Add), TestContext.CancellationToken);

            // Assert
            Assert.IsGreaterThan(1, reports.Count);
            Assert.AreEqual(1.0, reports[^1]);
            for (int i = 1; i < reports.Count; i++)
                Assert.IsGreaterThanOrEqualTo(reports[i - 1], reports[i]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private sealed class SynchronousProgress(Action<double> report) : IProgress<double>
    {
        public void Report(double value) => report(value);
    }

    #endregion

    #region FrameIndex

    [TestMethod]
    public async Task FrameIndex_ReadNextImage_AdvancesByOne()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act
        _ = _reader.ReadImage(out _);
        _ = _reader.ReadImage(out _);

        // Assert
        Assert.AreEqual<ulong>(2, _reader.FrameIndex);
    }

    [TestMethod]
    public async Task FrameIndex_ReadIndexedImage_PointsToNextFrame()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act
        _ = _reader.ReadImage(out _, 5);

        // Assert
        Assert.AreEqual<ulong>(6, _reader.FrameIndex);
    }

    [TestMethod]
    public async Task FrameIndex_Set_NextReadReturnsThatFrame()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act
        _reader.FrameIndex = 3;
        bool isRead = _reader.ReadImage(out GcBuffer buffer);

        // Assert
        Assert.IsTrue(isRead);
        Assert.AreEqual(_reader.GetTimeStamp(3), buffer.TimeStamp);
        Assert.AreEqual<ulong>(4, _reader.FrameIndex);
    }

    [TestMethod]
    public async Task FrameIndex_SetOutOfRange_ThrowsIndexOutOfRangeException()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act/Assert
        Assert.Throws<IndexOutOfRangeException>(() => _reader.FrameIndex = _reader.FrameCount);
        Assert.AreEqual<ulong>(0, _reader.FrameIndex);
    }

    [TestMethod]
    public async Task FrameIndex_AfterLastImage_EqualsFrameCount()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act
        while (_reader.ReadImage(out _)) { }

        // Assert
        Assert.AreEqual(_reader.FrameCount, _reader.FrameIndex);
    }

    #endregion

    #region Buffer content

    [TestMethod]
    public async Task ReadImage_ValidFile_BufferMatchesFileDescription()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act
        for (ulong i = 0; i < _reader.FrameCount; i++)
        {
            Assert.IsTrue(_reader.ReadImage(out GcBuffer buffer, i));

            // Assert
            Assert.AreEqual<uint>(320, buffer.Width);
            Assert.AreEqual<uint>(240, buffer.Height);
            Assert.AreEqual(PixelFormat.Mono8, buffer.PixelFormat);
            Assert.AreEqual((int)_reader.PayloadSize, buffer.ImageData.Length);
            Assert.AreEqual(_reader.GetTimeStamp(i), buffer.TimeStamp);
        }
    }

    [TestMethod]
    public async Task ReadImage_SameIndexTwice_ReturnsSeparateEqualBuffers()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act
        _ = _reader.ReadImage(out GcBuffer first, 2);
        _ = _reader.ReadImage(out GcBuffer second, 2);

        // Assert
        Assert.AreNotSame(first.ImageData, second.ImageData);
        Assert.AreEqual(first.FrameID, second.FrameID);
        CollectionAssert.AreEqual(first.ImageData, second.ImageData);
    }

    #endregion

    #region Dispose (more)

    [TestMethod]
    public async Task Dispose_CalledTwice_DoesNotThrow()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);

        // Act
        _reader.Dispose();
        _reader.Dispose();

        // Assert
        Assert.IsTrue(_reader.IsDisposed);
    }

    [TestMethod]
    public async Task Dispose_IsOpen_ReaderIsNoLongerOpenAndFrameIndexIsZero()
    {
        // Arrange
        _reader = new GcBufferReader(_pathToValidFile);
        await _reader.OpenAsync(token: TestContext.CancellationToken);
        _ = _reader.ReadImage(out _);

        // Act
        _reader.Dispose();

        // Assert
        Assert.IsFalse(_reader.IsOpen);
        Assert.AreEqual<ulong>(0, _reader.FrameIndex);
        Assert.AreEqual<ulong>(10, _reader.FrameCount);
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