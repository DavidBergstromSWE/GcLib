using System;
using System.IO;
using System.Reflection;
using System.Threading;
using GcLib.FileIO;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
[DoNotParallelize]
public class VideoWriterTests
{
    #region Fields

    private readonly string _path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"output.avi");
    private VideoWriter _writer;

    private event EventHandler<BufferTransferredEventArgs> BufferTransferred;

    #endregion

    #region TestConfiguration

    [TestInitialize]
    public void TestInitialize()
    {
        _writer = new VideoWriter(_path);

        BufferTransferred += _writer.OnBufferTransferred;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (_writer is not null && _writer.IsWriting)
        {
            BufferTransferred -= _writer.OnBufferTransferred;
            _writer.Dispose();
        }


        if (File.Exists(_path))
            File.Delete(_path);
    }

    #endregion

    #region Tests

    #region Constructor

    [TestMethod]
    public void VideoWriter_ValidateInitialization()
    {
        // Assert
        Assert.IsNotNull(_writer);
        Assert.AreEqual(0, _writer.BuffersQueued);
        Assert.AreEqual(0, _writer.FramesWritten);
        Assert.IsFalse(_writer.IsWriting);
        Assert.IsFalse(_writer.IsDisposed);
        Assert.AreEqual(_path, _writer.FilePath);
        Assert.AreEqual(0, _writer.FPS);
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

    [TestMethod]
    public void Start_OnBufferTransferred_BuffersAreQueued()
    {
        // Arrange
        _writer.Start();

        // Act
        int numBuffers = 10;
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Assert
        Assert.AreEqual(numBuffers, _writer.BuffersQueued);
    }

    [TestMethod]
    public void Start_OnBufferTransferred_BuffersAreWritten()
    {
        // Arrange
        _writer.Start();

        // Act
        int numBuffers = 40;
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));
        Thread.Sleep(300);

        // Assert
        Assert.AreEqual(numBuffers, _writer.FramesWritten);
    }

    #endregion

    #region Stop

    [TestMethod]
    public void Stop_IsWriting_IsNotWriting()
    {
        // Arrange
        _writer.Start();

        // Act
        _writer.Stop();

        // Assert
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    public void Stop_IsNotWriting_IsNotWriting()
    {
        // Act
        _writer.Stop();

        // Assert
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(20)]
    [DataRow(30)]
    [DataRow(40)]
    [DataRow(100)]
    public void Stop_WithoutDiscard_AllQueuedBuffersAreWritten(int numBuffers)
    {
        // Arrange
        _writer.Start();
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Act
        _writer.Stop(discardRemaining: false);
        Thread.Sleep(300);

        // Assert
        Assert.AreEqual(numBuffers, _writer.FramesWritten);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(20)]
    [DataRow(30)]
    [DataRow(40)]
    [DataRow(100)]
    public void Stop_WithDiscard_AllQueuedBuffersAreNotWritten(int numBuffers)
    {
        // Arrange
        _writer.Start();
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Act
        _writer.Stop(discardRemaining: true);
        Thread.Sleep(300);

        // Assert
        Assert.AreNotEqual(numBuffers, _writer.FramesWritten);
    }

    #endregion

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
    public void Dispose_IsWriting_IsDisposed()
    {
        // Arrange
        _writer.Start();

        // Act
        _writer.Dispose();

        // Assert
        Assert.IsTrue(_writer.IsDisposed);
        Assert.IsFalse(_writer.IsWriting);
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    #endregion

    #region Private methods

    private static GcBuffer GetBuffer(int n = 42)
    {
        byte[] data = TestPatternGenerator.CreateImage(width: 3,
                                                       height: 3,
                                                       pixelFormat: PixelFormat.Mono8,
                                                       testPattern: TestPattern.GrayHorizontalRamp,
                                                       frameNumber: 42);

        return new GcBuffer(imageData: data, width: 3, height: 3, pixelFormat: PixelFormat.Mono8, pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), frameID: n, timeStamp: (ulong)DateTime.Now.Ticks);
    }

    #endregion
}
