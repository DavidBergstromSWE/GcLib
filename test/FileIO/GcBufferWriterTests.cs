using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using GcLib.Utilities.FileIO;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

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
    public async Task GcBufferWriter_BareRelativeFilename_WritesInCurrentDirectory()
    {
        string filename = $"GcBufferWriter_{Guid.NewGuid():N}.bin";
        string fullPath = Path.GetFullPath(filename);
        try
        {
            using (var writer = new GcBufferWriter(filename))
            {
                Assert.AreEqual(filename, writer.FilePath);
                writer.Start();
                writer.OnBufferTransferred(this, new BufferTransferredEventArgs(GetBuffer()));
                await writer.StopAsync();
                Assert.AreEqual(1, writer.BuffersWritten);
            }

            Assert.IsTrue(File.Exists(fullPath));
            Assert.AreEqual((long)GcBufferWriter.ImageHeaderSize + GetBuffer().ImageData.Length, new FileInfo(fullPath).Length);
        }
        finally
        {
            File.Delete(fullPath);
        }
    }

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
        Assert.Throws<InvalidOperationException>(_writer.Start);
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

    #region WritingAborted

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WritingAborted_HandlerStopsWriter_DoesNotDeadlock(bool stopOnAnotherThread)
    {
        var streamField = typeof(GcBufferWriter).GetField("_fileStream", BindingFlags.Instance | BindingFlags.NonPublic);
        ((FileStream)streamField.GetValue(_writer)).Dispose();
        var failingStream = new FailingFileStream(_path);
        streamField.SetValue(_writer, failingStream);

        var completed = new TaskCompletionSource<WritingAbortedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (sender, args) =>
        {
            try
            {
                if (stopOnAnotherThread)
                    Task.Run(() => _writer.StopAsync()).WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                else
                    _writer.StopAsync().GetAwaiter().GetResult();

                completed.TrySetResult(args);
            }
            catch (Exception ex)
            {
                completed.TrySetException(ex);
            }
        };

        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));

        var eventArgs = await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.AreSame(failingStream.Failure, eventArgs.Exception);
        Assert.IsFalse(_writer.IsWriting);
        Assert.AreEqual(0, _writer.BuffersWritten);
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    #endregion

    #region IDisposable

    [TestMethod]
    [DataRow(0)]
    [DataRow(30)]
    public void Dispose_IsWriting_StopsRecordingThread(int numBuffers)
    {
        _writer.Start();
        var recordingThread = (System.Threading.Thread)typeof(GcBufferWriter)
            .GetField("_recordingThread", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_writer);

        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        _writer.Dispose();

        Assert.IsFalse(recordingThread.IsAlive);
        Assert.IsTrue(_writer.IsDisposed);
        Assert.IsFalse(_writer.IsWriting);
        Assert.AreEqual(0, _writer.BuffersQueued);

        using var file = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

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

    private sealed class FailingFileStream(string path) : FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)
    {
        public IOException Failure { get; } = new("Simulated write failure.");

        public override void Write(byte[] buffer, int offset, int count) => throw Failure;
    }

    private static GcBuffer GetBuffer(int n = 42)
    {
        byte[] data = TestPatternGenerator.CreateImage(width: 3,
                                                        height: 3,
                                                        pixelFormat: PixelFormat.Mono8,
                                                        testPattern: TestPattern.GrayHorizontalRamp,
                                                        frameNumber: 42);

        return new GcBuffer(imageData: data, width: 3, height: 3, pixelFormat: PixelFormat.Mono8, frameID: n, timeStamp: (ulong)DateTime.Now.Ticks);
    }

    #endregion
}