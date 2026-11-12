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

    #region Restart

    [TestMethod]
    public async Task Start_AfterStopAsync_WritesBuffersOfSecondRun()
    {
        // Arrange
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(0)));
        await _writer.StopAsync();

        // Act
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(1)));
        await _writer.StopAsync();

        // Assert
        Assert.AreEqual(2, _writer.BuffersWritten);
        Assert.AreEqual(0, _writer.BuffersQueued);
        Assert.AreEqual(2 * ((long)GcBufferWriter.ImageHeaderSize + 9), _writer.FileSize);
    }

    [TestMethod]
    public void OnBufferTransferred_IsNotWriting_BufferIsIgnored()
    {
        // Act
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));

        // Assert
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    [TestMethod]
    public async Task StopAsync_CalledConcurrently_WritesEachBufferOnce()
    {
        // Arrange
        int numBuffers = 100;
        _writer.Start();
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Act
        await Task.WhenAll(Task.Run(() => _writer.StopAsync()), Task.Run(() => _writer.StopAsync()), _writer.StopAsync());

        // Assert
        Assert.IsFalse(_writer.IsWriting);
        Assert.AreEqual(numBuffers, _writer.BuffersWritten);
        Assert.AreEqual(numBuffers * ((long)GcBufferWriter.ImageHeaderSize + 9), _writer.FileSize);
    }

    [TestMethod]
    public async Task WritingAborted_ExceptionOtherThanIOException_AbortsWithoutCrashing()
    {
        // Arrange
        var streamField = typeof(GcBufferWriter).GetField("_fileStream", BindingFlags.Instance | BindingFlags.NonPublic);
        ((FileStream)streamField.GetValue(_writer)).Dispose(); // Writing to a closed stream throws ObjectDisposedException.

        var aborted = new TaskCompletionSource<WritingAbortedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, args) => aborted.TrySetResult(args);
        _writer.Start();

        // Act
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));
        var eventArgs = await aborted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));
        await _writer.StopAsync();

        // Assert
        Assert.IsInstanceOfType<ObjectDisposedException>(eventArgs.Exception);
        Assert.AreEqual(0, _writer.BuffersWritten);
        Assert.AreEqual(0, _writer.BuffersQueued);
        Assert.IsFalse(_writer.IsWriting);
    }

    #endregion

    #region File format

    [TestMethod]
    public async Task StopAsync_ImageDataLargerThanImage_WritesOnlyImageAndIsReadable()
    {
        // Arrange
        byte[] oversized = new byte[20];
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(new GcBuffer(oversized, 3, 3, PixelFormat.Mono8, 1, 100)));
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(new GcBuffer(oversized, 3, 3, PixelFormat.Mono8, 2, 200)));

        // Act
        await _writer.StopAsync();
        _writer.Dispose();

        // Assert
        Assert.AreEqual(2 * ((long)GcBufferWriter.ImageHeaderSize + 9), new FileInfo(_path).Length);
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync();
        Assert.AreEqual<ulong>(2, reader.FrameCount);
    }

    [TestMethod]
    public async Task StopAsync_PackedImageWithOddSize_IsReadable()
    {
        // Arrange (5x5 pixels at 12 bits per pixel require 37.5 bytes, rounded up to 38).
        byte[] data = new byte[38];
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(new GcBuffer(data, 5, 5, PixelFormat.Mono12p, 1, 100)));
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(new GcBuffer(data, 5, 5, PixelFormat.Mono12p, 2, 200)));

        // Act
        await _writer.StopAsync();
        _writer.Dispose();

        // Assert
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync();
        Assert.AreEqual<ulong>(2, reader.FrameCount);
        Assert.IsTrue(reader.ReadImage(out GcBuffer buffer, 1));
        Assert.AreEqual(2, buffer.FrameID);
        Assert.AreEqual<uint>(5, buffer.Width);
    }

    [TestMethod]
    public async Task WritingAborted_BufferWithDifferentSize_AbortsWriting()
    {
        // Arrange
        var aborted = new TaskCompletionSource<WritingAbortedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, args) => aborted.TrySetResult(args);
        _writer.Start();

        // Act
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(0)));
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(new GcBuffer(new byte[16], 4, 4, PixelFormat.Mono8, 1, 100)));
        var eventArgs = await aborted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _writer.StopAsync();

        // Assert
        Assert.IsInstanceOfType<InvalidOperationException>(eventArgs.Exception);
        Assert.AreEqual(1, _writer.BuffersWritten);
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    #endregion

    #region Constructor (file handling)

    [TestMethod]
    public void GcBufferWriter_NonExistingDirectory_CreatesDirectoryAndFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"GcBufferWriter_{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "nested", "output.bin");
        try
        {
            using (var writer = new GcBufferWriter(path))
            {
                Assert.IsTrue(File.Exists(path));
            }
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [TestMethod]
    public async Task GcBufferWriter_ExistingFile_IsOverwritten()
    {
        string path = Path.Combine(Path.GetTempPath(), $"GcBufferWriter_{Guid.NewGuid():N}.bin");
        try
        {
            await File.WriteAllBytesAsync(path, new byte[1000]);

            using (var writer = new GcBufferWriter(path))
            {
                writer.Start();
                writer.OnBufferTransferred(this, new BufferTransferredEventArgs(GetBuffer()));
                await writer.StopAsync();
            }

            Assert.AreEqual((long)GcBufferWriter.ImageHeaderSize + 9, new FileInfo(path).Length);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [TestMethod]
    public void GcBufferWriter_FileIsLockedUntilDisposed()
    {
        // Act/Assert
        Assert.Throws<IOException>(() => { using var file = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); });

        _writer.Dispose();

        using var released = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None);
    }

    #endregion

    #region File format

    [TestMethod]
    public async Task StopAsync_SingleBuffer_FileContainsImageHeaderAndImageData()
    {
        // Arrange
        byte[] data = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        var buffer = new GcBuffer(data, 3, 3, PixelFormat.Mono8, frameID: 123456789012L, timeStamp: 987654321098UL);
        _writer.Start();

        // Act
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(buffer));
        await _writer.StopAsync();
        _writer.Dispose();

        // Assert
        byte[] file = File.ReadAllBytes(_path);
        Assert.AreEqual((int)GcBufferWriter.ImageHeaderSize + data.Length, file.Length);
        Assert.AreEqual(123456789012L, BitConverter.ToInt64(file, 0));
        Assert.AreEqual(987654321098UL, BitConverter.ToUInt64(file, 8));
        Assert.AreEqual(3u, BitConverter.ToUInt32(file, 16));
        Assert.AreEqual(3u, BitConverter.ToUInt32(file, 20));
        Assert.AreEqual((int)PixelFormat.Mono8, BitConverter.ToInt32(file, 24));
        Assert.AreEqual(buffer.PixelDynamicRangeMax, BitConverter.ToUInt32(file, 28));
        CollectionAssert.AreEqual(data, file[(int)GcBufferWriter.ImageHeaderSize..]);
    }

    [TestMethod]
    public async Task StopAsync_MultipleBuffers_BuffersAreWrittenInOrder()
    {
        // Arrange
        int numBuffers = 50;
        _writer.Start();
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Act
        await _writer.StopAsync();
        _writer.Dispose();

        // Assert
        byte[] file = File.ReadAllBytes(_path);
        int frameSize = (int)GcBufferWriter.ImageHeaderSize + 9;
        Assert.AreEqual(numBuffers * frameSize, file.Length);
        for (int i = 0; i < numBuffers; i++)
            Assert.AreEqual((long)i, BitConverter.ToInt64(file, i * frameSize));
    }

    #endregion

    #region Thread

    [TestMethod]
    public void Start_ThreadPriority_IsAppliedToBackgroundRecordingThread()
    {
        // Arrange
        _writer.ThreadPriority = System.Threading.ThreadPriority.Highest;

        // Act
        _writer.Start();

        // Assert
        var recordingThread = (System.Threading.Thread)typeof(GcBufferWriter)
            .GetField("_recordingThread", BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(_writer);
        Assert.AreEqual(System.Threading.ThreadPriority.Highest, recordingThread.Priority);
        Assert.IsTrue(recordingThread.IsBackground);
    }

    #endregion

    #region WritingAborted (more)

    [TestMethod]
    public async Task WritingAborted_MultipleBuffersFail_RaisedOnce()
    {
        // Arrange
        _ = ReplaceStream(() => new FailingFileStream(_path));
        int raised = 0;
        var first = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, _) =>
        {
            _ = System.Threading.Interlocked.Increment(ref raised);
            first.TrySetResult();
        };
        _writer.Start();

        // Act
        for (int i = 0; i < 10; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));
        await first.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _writer.StopAsync();
        await Task.Delay(200);

        // Assert
        Assert.AreEqual(1, raised);
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    [TestMethod]
    public async Task WritingAborted_AfterAbort_BuffersAreIgnored()
    {
        // Arrange
        _ = ReplaceStream(() => new FailingFileStream(_path));
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, _) => aborted.TrySetResult();
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));
        await aborted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Act
        for (int i = 0; i < 10; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Assert (the writer is still writing until stopped, but does not accumulate buffers).
        Assert.IsTrue(_writer.IsWriting);
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    [TestMethod]
    public async Task WritingAborted_FlushFailsOnStop_IsRaised()
    {
        // Arrange
        var stream = ReplaceStream(() => new FlushFailingFileStream(_path));
        var aborted = new TaskCompletionSource<WritingAbortedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, args) => aborted.TrySetResult(args);
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));

        // Act
        await _writer.StopAsync();

        // Assert
        var eventArgs = await aborted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.AreSame(stream.Failure, eventArgs.Exception);
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    public async Task WritingAborted_HandlerThrows_DoesNotCrash()
    {
        // Arrange
        _ = ReplaceStream(() => new FailingFileStream(_path));
        var raised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, _) =>
        {
            raised.TrySetResult();
            throw new InvalidOperationException("Handler failure.");
        };
        _writer.Start();

        // Act
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));
        await raised.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _writer.StopAsync();

        // Assert
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    public async Task Start_AfterAbort_WritesBuffersOfNewRun()
    {
        // Arrange
        _ = ReplaceStream(() => new FailingFileStream(_path));
        var aborted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, _) => aborted.TrySetResult();
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(0)));
        await aborted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await _writer.StopAsync();

        _ = ReplaceStream(() => new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true));

        // Act
        _writer.Start();
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(1)));
        await _writer.StopAsync();

        // Assert
        Assert.AreEqual(1, _writer.BuffersWritten);
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    #endregion

    #region IDisposable (more)

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        // Act
        _writer.Dispose();
        _writer.Dispose();

        // Assert
        Assert.IsTrue(_writer.IsDisposed);
    }

    [TestMethod]
    public async Task StopAsync_IsDisposed_DoesNothing()
    {
        // Arrange
        _writer.Start();
        _writer.Dispose();

        // Act/Assert
        await _writer.StopAsync();
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    public void OnBufferTransferred_IsDisposed_DoesNotThrow()
    {
        // Arrange
        _writer.Start();
        _writer.Dispose();

        // Act/Assert
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    [TestMethod]
    public async Task Dispose_HandlerDisposesWriter_DoesNotDeadlock()
    {
        // Arrange
        _ = ReplaceStream(() => new FailingFileStream(_path));
        var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _writer.WritingAborted += (_, _) =>
        {
            try
            {
                _writer.Dispose();
                disposed.TrySetResult();
            }
            catch (Exception ex)
            {
                disposed.TrySetException(ex);
            }
        };
        _writer.Start();

        // Act
        BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer()));
        await disposed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        Assert.IsTrue(_writer.IsDisposed);
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    public void Dispose_IsWritingOnThreadWithSynchronizationContext_DoesNotDeadlock()
    {
        // Arrange
        _writer.Start();
        for (int i = 0; i < 10; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        var thread = new System.Threading.Thread(() =>
        {
            System.Threading.SynchronizationContext.SetSynchronizationContext(new NeverPumpedSynchronizationContext());
            _writer.Dispose();
        })
        { IsBackground = true };

        // Act
        thread.Start();

        // Assert
        Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(5)), "Dispose did not complete (deadlock).");
        Assert.IsTrue(_writer.IsDisposed);
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

    private sealed class FlushFailingFileStream(string path) : FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)
    {
        public IOException Failure { get; } = new("Simulated flush failure.");

        public override void Flush() => throw Failure;
    }

    private sealed class NeverPumpedSynchronizationContext : System.Threading.SynchronizationContext
    {
        public override void Post(System.Threading.SendOrPostCallback d, object state) { }
    }

    /// <summary>
    /// Replaces the file stream of the writer under test (disposing the previous one).
    /// </summary>
    private T ReplaceStream<T>(Func<T> createStream) where T : FileStream
    {
        var streamField = typeof(GcBufferWriter).GetField("_fileStream", BindingFlags.Instance | BindingFlags.NonPublic);
        ((FileStream)streamField.GetValue(_writer)).Dispose(); // Must be released before the file can be reopened.
        var stream = createStream();
        streamField.SetValue(_writer, stream);
        return stream;
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