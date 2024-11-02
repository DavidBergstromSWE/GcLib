using System;
using System.Threading;
using GcLib.UnitTests.Utilities;
using GcLib.Utilities.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests;

[TestClass]
public class GcProcessingThreadTests
{
    private Mock<IBufferStream> _mockStream;
    private GcProcessingThread _processingThread;


    [TestInitialize]
    public void TestInitialize()
    {
        _mockStream = new Mock<IBufferStream>();
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

        Assert.IsTrue(_processingThread != null);
        Assert.IsTrue(_processingThread.QueuedCount == 0);
        Assert.IsTrue(_processingThread.FPS == 0.0);
        Assert.IsTrue(_processingThread.LimitFPS == limitFPS);
        Assert.IsFalse(_processingThread.IsRunning);
        Assert.IsTrue(_processingThread.ID == ID);
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
    public void StopAwait_AcquireSingleImage_BufferIsNotNull()
    {
        // Arrange
        GcBuffer expectedBuffer = null;
        _processingThread = new GcProcessingThread();
        _processingThread.BufferProcess += (sender, buffer) => { expectedBuffer = buffer; };         
        _processingThread.Start(_mockStream.Object);

        // Act
        _mockStream.Raise(s => s.BufferTransferred += null, new BufferTransferredEventArgs(FakeBufferProvider.GetFakeBuffer()));
        _processingThread.Stop(true);

        // Assert
        Assert.IsNotNull(expectedBuffer);
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
        _mockStream.Raise(s => s.BufferTransferred += null, new BufferTransferredEventArgs(FakeBufferProvider.GetFakeBuffer()));
        Thread.Sleep(100);
        _processingThread.Stop();
        _processingThread.WaitComplete();

        // Assert
        Assert.IsNotNull(expectedBuffer);        
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
            _mockStream.Raise(s => s.BufferTransferred += null, new BufferTransferredEventArgs(FakeBufferProvider.GetFakeBuffer(i)));

        // Assert
        Assert.IsTrue(eventCounter > 0);
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
        Assert.IsTrue(_processingThread.QueuedCount == 0);
    }
}