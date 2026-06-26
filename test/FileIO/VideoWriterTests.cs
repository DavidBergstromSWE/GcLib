using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GcLib.FileIO;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
[DoNotParallelize]
public class VideoWriterTests
{
    #region Fields

    private readonly string _path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"output.mp4");
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
        if (_writer != null)
            BufferTransferred -= _writer.OnBufferTransferred;

        if (_writer != null && _writer.IsDisposed == false)
            _writer.Dispose();

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
        Thread.Sleep(100);

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
        Assert.IsTrue(File.Exists(_path));
    }

    #endregion

    #region Stop

    [TestMethod]
    public async Task StopAsync_IsWriting_IsNotWriting()
    {
        // Arrange
        _writer.Start();

        // Act
        await _writer.StopAsync();

        // Assert
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    public async Task StopAsync_IsNotWriting_IsNotWriting()
    {
        // Act
        await _writer.StopAsync();

        // Assert
        Assert.IsFalse(_writer.IsWriting);
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(30)]
    [DataRow(60)]
    public async Task StopAsync_AllQueuedBuffersAreWritten(int numBuffers)
    {
        // Arrange
        _writer.Start();
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Act
        await _writer.StopAsync();

        // Assert
        Assert.AreEqual(numBuffers, _writer.FramesWritten);
        Assert.AreEqual(0, _writer.BuffersQueued);
        Assert.IsFalse(_writer.IsWriting);
        Assert.IsTrue(File.Exists(_path));
    }

    [TestMethod]
    [DataRow(10)]
    [DataRow(30)]
    [DataRow(60)]
    public async Task StopAsync_FPS_ValidateAverage(int numBuffers)
    {
        // Arrange
        _writer.Start();
        var timer = new System.Timers.Timer(1 / 30.0 * 1000) { AutoReset = true };
        var i = 0;
        timer.Elapsed += (s, e) => { BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(++i))); };
        timer.Start();
        while (i < numBuffers) { }
        timer.Stop();

        // Act
        await _writer.StopAsync();

        // Assert
        Assert.IsLessThan(35, _writer.FPS);
        Assert.IsGreaterThan(25, _writer.FPS);

        timer.Dispose();
    }

    [TestMethod]
    [DataRow(VideoWriter.CODEC.MJPEG)]
    [DataRow(VideoWriter.CODEC.H264)]
    //[DataRow(VideoWriter.CODEC.H265)]
    public async Task StopAsync_FormatIsSupported_BuffersAreWritten(VideoWriter.CODEC codec)
    {
        // Arrange
        var writer = new VideoWriter(_path, 30, codec);
        BufferTransferred += writer.OnBufferTransferred;
        writer.Start();
        int numBuffers = 30;
        for (int i = 0; i < numBuffers; i++)
            BufferTransferred.Invoke(this, new BufferTransferredEventArgs(GetBuffer(i)));

        // Act
        await writer.StopAsync();

        // Assert
        Assert.AreEqual(numBuffers, writer.FramesWritten);
        Assert.IsTrue(File.Exists(_path));

        BufferTransferred -= writer.OnBufferTransferred;
        writer.Dispose();
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
    public void Dispose_IsWriting_IsNotWritingAndIsDisposed()
    {
        // Arrange
        _writer.Start();

        // Act
        _writer.Dispose();

        // Assert
        Assert.IsFalse(_writer.IsWriting);
        Assert.IsTrue(_writer.IsDisposed);        
        Assert.AreEqual(0, _writer.BuffersQueued);
    }

    #endregion

    #endregion

    #region Private methods

    private static GcBuffer GetBuffer(int n = 42, uint width = 640, uint height = 320)
    {
        byte[] data = TestPatternGenerator.CreateImage(width: width,
                                                       height: height,
                                                       pixelFormat: PixelFormat.Mono8,
                                                       testPattern: TestPattern.GrayHorizontalRamp,
                                                       frameNumber: 42);

        return new GcBuffer(imageData: data, width: width, height: height, pixelFormat: PixelFormat.Mono8, pixelDynamicRangeMax: GenICamHelper.GetPixelDynamicRangeMax(PixelFormat.Mono8), frameID: n, timeStamp: (ulong)DateTime.Now.Ticks);
    }

    #endregion
}
