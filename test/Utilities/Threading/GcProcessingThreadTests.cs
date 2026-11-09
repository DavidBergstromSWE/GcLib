using System;
using System.Threading;
using GcLib.Utilities.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests.Utilities;

[TestClass]
public class GcProcessingThreadTests
{
    private Mock<IBufferStream> _mockStream;
    private GcProcessingThread _processingThread;
    private GcBuffer _buffer;


    [TestInitialize]
    public void TestInitialize()
    {
        _mockStream = new Mock<IBufferStream>();
        _buffer = FakeBufferProvider.GetFakeBuffer();
        GcBuffer buffer = _buffer;
        _mockStream.Setup(s => s.RetrieveBuffer(out buffer, 0)).Returns(true);
    }

    [TestCleanup]
    public void TestCleanUp()
    {
        _processingThread?.Stop();
        _processingThread?.Dispose();
        _processingThread = null;

        _mockStream.Reset();
    }

    [TestMethod]
    public void Constructor_ValidateInitialization()
    {
        // Act
        int bufferCapacity = 4;
        bool limitFPS = false;
        string ID = Guid.NewGuid().ToString();
        _processingThread = new GcProcessingThread(bufferCapacity: bufferCapacity, limitFPS: limitFPS, ID: ID);

        Assert.IsNotNull(_processingThread);
        Assert.AreEqual(0, _processingThread.QueuedCount);
        Assert.AreEqual(0.0, _processingThread.FPS);
        Assert.AreEqual(limitFPS, _processingThread.LimitFPS);
        Assert.IsFalse(_processingThread.IsRunning);
        Assert.AreEqual(ID, _processingThread.ID);
    }

    [TestMethod]
    public void Start_IsRunning()
    {
        // Arrange
        _processingThread = new GcProcessingThread();

        // Act
        _processingThread.Start(_mockStream.Object);

        // Assert
        Assert.IsTrue(_processingThread.IsRunning);
    }

    [TestMethod]
    public void Start_NullStream_ThrowsArgumentNullException()
    {
        // Arrange
        _processingThread = new GcProcessingThread();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _processingThread.Start(dataStream: null));
    }

    [TestMethod]
    public void Start_WhileAlreadyRunning_ThrowsInvalidOperationException()
    {
        // Arrange
        _processingThread = new GcProcessingThread();
        _processingThread.Start(_mockStream.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _processingThread.Start(_mockStream.Object));
    }

    [TestMethod]
    public void Start_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        _processingThread = new GcProcessingThread();
        _processingThread.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _processingThread.Start(_mockStream.Object));
    }

    [TestMethod]
    public void Stop_WhenNotRunning_DoesNotThrow()
    {
        // Arrange
        _processingThread = new GcProcessingThread();

        // Act
        _processingThread.Stop();

        // Assert
        Assert.IsFalse(_processingThread.IsRunning);
    }

    [TestMethod]
    public void Stop_WhileRunning_IsNotRunning()
    {
        // Arrange
        _processingThread = new GcProcessingThread();
        _processingThread.Start(_mockStream.Object);

        // Act
        _processingThread.Stop();

        // Assert
        Assert.IsFalse(_processingThread.IsRunning);
    }

    [TestMethod]
    public void StopAwait_WhileRunning_IsNotRunning()
    {
        // Arrange
        _processingThread = new GcProcessingThread();
        _processingThread.Start(_mockStream.Object);

        // Act
        _processingThread.Stop(true);

        // Assert
        Assert.IsFalse(_processingThread.IsRunning);
    }

    [TestMethod]
    public void StopFalse_WithQueuedBuffers_ClearsQueue()
    {
        // Arrange
        _processingThread = new GcProcessingThread(bufferCapacity: 8);
        _processingThread.BufferProcess += (sender, buffer) => Thread.Sleep(500);
        _processingThread.Start(_mockStream.Object);

        for (int i = 0; i < 8; i++)
            _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);

        // Act
        _processingThread.Stop(aWait: false);

        // Assert
        Assert.AreEqual(0, _processingThread.QueuedCount);
    }

    [TestMethod]
    public void StopAwait_AcquireSingleImage_BufferIsNotNull()
    {
        // Arrange
        GcBuffer expectedBuffer = null;
        _processingThread = new GcProcessingThread();
        _processingThread.BufferProcess += (sender, buffer) => { expectedBuffer = buffer; };
        _processingThread.Start(_mockStream.Object);

        // Act
        _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);
        Thread.Sleep(100);
        _processingThread.Stop(true);

        // Assert
        Assert.IsNotNull(expectedBuffer);
        Assert.AreSame(expectedBuffer, _buffer);
    }

    [TestMethod]
    public void Stop_ThenRaiseNewBuffer_DoesNotProcessBuffer()
    {
        // Arrange
        _processingThread = new GcProcessingThread();
        int processedCount = 0;
        _processingThread.BufferProcess += (sender, buffer) => Interlocked.Increment(ref processedCount);
        _processingThread.Start(_mockStream.Object);
        _processingThread.Stop(aWait: true);

        // Act
        _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);
        Thread.Sleep(100);

        // Assert
        Assert.AreEqual(0, processedCount);
        _mockStream.Verify(s => s.RetrieveBuffer(out It.Ref<GcBuffer>.IsAny, 0), Times.Never);
    }

    [TestMethod]
    public void WaitComplete_WithoutStarting_DoesNotThrow()
    {
        // Arrange
        _processingThread = new GcProcessingThread();

        // Act
        _processingThread.WaitComplete();

        // Assert
        Assert.IsFalse(_processingThread.IsRunning);
    }

    [TestMethod]
    public void StopWaitComplete_AcquireSingleImage_BufferIsNotNull()
    {
        // Arrange
        GcBuffer expectedBuffer = null;
        _processingThread = new GcProcessingThread();
        _processingThread.BufferProcess += (sender, buffer) => { expectedBuffer = buffer; };
        _processingThread.Start(_mockStream.Object);

        // Act
        _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);
        Thread.Sleep(100);
        _processingThread.Stop();
        _processingThread.WaitComplete();

        // Assert
        Assert.IsNotNull(expectedBuffer);
        Assert.AreSame(expectedBuffer, _buffer);
    }

    [TestMethod]
    public void SlowProcessing_AcquireImagesOverCapacity_BufferOverFlowIsRaised()
    {
        // Arrange
        int capacity = 2;
        _processingThread = new GcProcessingThread(bufferCapacity: capacity);
        _processingThread.BufferProcess += (sender, buffer) => { Thread.Sleep(5000); };

        int eventCounter = 0;
        _processingThread.BufferOverFlow += (sender, eventArgs) => { eventCounter++; };

        // Act
        _processingThread.Start(_mockStream.Object);
        for (int i = 0; i < capacity + 2; i++)
            _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);

        // Assert
        Assert.IsGreaterThan(0, eventCounter);
    }

    [TestMethod]
    public void LimitFPS_BurstInput_DropsFrames()
    {
        // Arrange
        _processingThread = new GcProcessingThread(bufferCapacity: 16, limitFPS: true, targetFPS: 1.0);
        int processedCount = 0;
        _processingThread.BufferProcess += (sender, buffer) => Interlocked.Increment(ref processedCount);
        _processingThread.Start(_mockStream.Object);

        // Act
        for (int i = 0; i < 30; i++)
            _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);

        Thread.Sleep(300);
        _processingThread.Stop(aWait: true);

        // Assert
        Assert.IsTrue(processedCount > 0);
        Assert.IsTrue(processedCount < 30);
    }

    [TestMethod]
    public void BufferProcessHandler_ThrowsException_ThreadKeepsRunning()
    {
        // Arrange
        _processingThread = new GcProcessingThread();
        ManualResetEventSlim handlerWasCalled = new();
        _processingThread.BufferProcess += (sender, buffer) =>
        {
            handlerWasCalled.Set();
            throw new InvalidOperationException("Expected test exception");
        };
        _processingThread.Start(_mockStream.Object);

        // Act
        _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);

        // Assert
        Assert.IsTrue(handlerWasCalled.Wait(1000));
        Assert.IsTrue(_processingThread.IsRunning);

        _processingThread.Stop(true);
        Assert.IsFalse(_processingThread.IsRunning);
    }

    [TestMethod]
    public void NewBuffer_RetrievalFails_DoesNotQueueOrProcessBuffer()
    {
        GcBuffer buffer = null;
        _mockStream.Setup(s => s.RetrieveBuffer(out buffer, 0)).Returns(false);
        _processingThread = new GcProcessingThread();
        int processedCount = 0;
        _processingThread.BufferProcess += (sender, image) => Interlocked.Increment(ref processedCount);
        _processingThread.Start(_mockStream.Object);

        _mockStream.Raise(s => s.NewBuffer += null, EventArgs.Empty);
        _processingThread.Stop(aWait: true);

        _mockStream.Verify(s => s.RetrieveBuffer(out It.Ref<GcBuffer>.IsAny, 0), Times.Once);
        Assert.AreEqual(0, _processingThread.QueuedCount);
        Assert.AreEqual(0, processedCount);
    }

    [TestMethod]
    public void Dispose_ValidateState()
    {
        // Arrange
        _processingThread = new GcProcessingThread();
        _processingThread.Start(_mockStream.Object);

        // Act
        _processingThread.Dispose();

        // Assert
        Assert.AreEqual(0, _processingThread.QueuedCount);
    }
}