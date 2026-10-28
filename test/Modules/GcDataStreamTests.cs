using System;
using System.Diagnostics;
using GcLib.UnitTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests.Modules;

[TestClass]
public class GcDataStreamTests
{
    #region Fields

    private GcDataStream _dataStream;

    /// <summary>
    /// Mocked acquirer of buffers.
    /// </summary>
    private readonly Mock<IBufferProducer> _mockDevice = new();

    /// <summary>
    /// Buffer data for testing (content unimportant).
    /// </summary>
    private GcBuffer _testBuffer;

    #endregion

    #region TestConfiguration

    [TestInitialize]
    public void TestInitialize()
    {
        // Mock behaviour of device.
        _mockDevice.Setup(d => d.StartAcquisition()).Raises(d => d.AcquisitionStarted += null, EventArgs.Empty);
        _mockDevice.Setup(d => d.StopAcquisition()).Raises(d => d.AcquisitionStopped += null, EventArgs.Empty);

        // Generate simple buffer data for testing.
        _testBuffer = FakeBufferProvider.GetFakeBuffer(3, 3, PixelFormat.Mono8);
    }

    [TestCleanup]
    public void TestCleanUp()
    {
        _dataStream.Stop();
        _dataStream.Close();
        _dataStream = null;

        _mockDevice.Reset();

        _testBuffer = null;
    }

    #endregion

    #region ConstructorTests

    [TestMethod]
    public void Constructor_ValidateInstantiation()
    {
        // Arrange
        string streamID = Guid.NewGuid().ToString();
        int capacity = 2;

        // Act
        _dataStream = new GcDataStream(_mockDevice.Object, streamID, capacity);

        // Assert
        Assert.IsNotNull(_dataStream);

        Assert.AreEqual(streamID, _dataStream.StreamID);
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
        Assert.AreEqual<ulong>(0, _dataStream.DeliveredFrameCount);
        Assert.AreEqual<ulong>(0, _dataStream.LostFrameCount);
        Assert.AreEqual<ulong>(0, _dataStream.FailedFrameCount);
        Assert.AreEqual(0, _dataStream.FrameRate);
        Assert.AreEqual(0, _dataStream.FrameRateAverage);
        Assert.AreEqual(capacity, (int)_dataStream.OutputBufferQueueCapacity);

        Assert.IsFalse(_dataStream.IsStreaming);
        Assert.IsTrue(_dataStream.IsOpen);
    }

    #endregion

    #region MethodTests

    [TestMethod]
    public void RetrieveImage_QueueIsEmpty_BufferIsNull()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act
        var success = _dataStream.RetrieveImage(out GcBuffer buffer, 0);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(buffer);
    }

    [TestMethod]
    public void RetrieveImage_QueueHasBuffer_ReturnsBuffer()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();
        _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(_testBuffer, DateTime.Now));

        // Act
        var success = _dataStream.RetrieveImage(out GcBuffer buffer, 0);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(buffer);
        Assert.AreSame(_testBuffer, buffer);
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
    }

    [TestMethod]
    public void RetrieveImage_NoBufferWithinTimeout_ReturnsFalseAfterTimeout()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        const int timeout = 50;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var success = _dataStream.RetrieveImage(out GcBuffer buffer, timeout);
        stopwatch.Stop();

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(buffer);
        Assert.IsTrue(stopwatch.ElapsedMilliseconds >= timeout - 5); // Allowing a small margin for timing inaccuracies
    }

    [TestMethod]
    public void RetrieveImage_QueueHasMultipleBuffers_BuffersAreRetrievedInOrder()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        var firstBuffer = FakeBufferProvider.GetFakeBuffer();
        var secondBuffer = FakeBufferProvider.GetFakeBuffer();

        _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(firstBuffer, DateTime.Now));
        _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(secondBuffer, DateTime.Now));

        // Act
        var firstSuccess = _dataStream.RetrieveImage(out GcBuffer retrievedFirstBuffer, 0);
        var secondSuccess = _dataStream.RetrieveImage(out GcBuffer retrievedSecondBuffer, 0);

        // Assert
        Assert.IsTrue(firstSuccess);
        Assert.IsTrue(secondSuccess);
        Assert.AreSame(firstBuffer, retrievedFirstBuffer);
        Assert.AreSame(secondBuffer, retrievedSecondBuffer);
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
    }

    [TestMethod]
    public void Start_IsNotStreaming_IsStreaming()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act
        _dataStream.Start();

        // Assert
        Assert.IsTrue(_dataStream.IsStreaming);
    }

    [TestMethod]
    public void Start_IsNotStreaming_StatisticsIsReset()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act
        _dataStream.Start();

        // Assert
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
        Assert.AreEqual<ulong>(0, _dataStream.DeliveredFrameCount);
        Assert.AreEqual<ulong>(0, _dataStream.LostFrameCount);
        Assert.AreEqual<ulong>(0, _dataStream.FailedFrameCount);
        Assert.AreEqual(0, _dataStream.FrameRate);
        Assert.AreEqual(0, _dataStream.FrameRateAverage);
    }

    [TestMethod]
    public void Start_IsNotStreaming_AcquisitionStartedIsRaisedInDevice()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        int eventCounter = 0;
        _mockDevice.Object.AcquisitionStarted += (s, e) => { eventCounter++; };

        // Act
        _dataStream.Start();

        // Assert
        Assert.AreEqual(1, eventCounter);
    }

    [TestMethod]
    public void Start_IsNotStreaming_StreamingStartedIsRaised()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        int eventCounter = 0;
        _dataStream.StreamingStarted += (s, e) => { eventCounter++; };

        // Act
        _dataStream.Start();

        // Assert
        Assert.AreEqual(1, eventCounter);
    }

    [TestMethod]
    public void Start_IsAlreadyStreaming_ThrowsInvalidOperationException()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => _dataStream.Start());
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    public void Start_AcquireSpecifiedNumber_StreamingIsStopped(int count)
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString(), count);
        int eventCounter = 0;
        _dataStream.StreamingStopped += (s, e) => { eventCounter++; };

        // Act
        _dataStream.Start((ulong)count);
        for (int i = 0; i < count; i++)
            _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(_testBuffer, DateTime.Now));

        // Assert
        Assert.IsFalse(_dataStream.IsStreaming);
        Assert.AreEqual(1, eventCounter);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    public void Start_AcquireMultipleImages_BuffersAreDeliveredAndQueued(int count)
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString(), count);

        // Act
        _dataStream.Start((ulong)count);
        for (int i = 0; i < count; i++)
            _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(_testBuffer, DateTime.Now));

        // Assert
        Assert.AreEqual((ulong)count, _dataStream.DeliveredFrameCount);
        Assert.AreEqual(count, _dataStream.AwaitDeliveryCount);
    }

    [TestMethod]
    public void Stop_IsStreaming_AcquisitionStoppedIsRaisedInDevice()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();
        int eventCounter = 0;

        _mockDevice.Object.AcquisitionStopped += (s, e) => { eventCounter++; };

        // Act
        _dataStream.Stop();

        // Assert
        Assert.AreEqual(1, eventCounter);
    }

    [TestMethod]
    public void Stop_IsStreaming_StreamingIsStopped()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();
        int eventCounter = 0;
        _dataStream.StreamingStopped += (s, e) => { eventCounter++; };

        // Act
        _dataStream.Stop();

        // Assert
        Assert.IsFalse(_dataStream.IsStreaming);
        Assert.AreEqual(1, eventCounter);
    }

    [TestMethod]
    public void Close_IsNotStreaming_IsNotOpen()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act
        _dataStream.Close();

        // Assert
        Assert.IsFalse(_dataStream.IsOpen);
    }

    [TestMethod]
    public void Close_IsStreaming_StreamingIsStopped()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        _dataStream.Close();

        // Assert
        Assert.IsFalse(_dataStream.IsStreaming);
    }

    [TestMethod]
    public void GetParentDevice_IsGcDevice_ReturnsDevice()
    {
        // Arrange
        var expectedDevice = new VirtualCam("TestDevice");
        _dataStream = new GcDataStream(expectedDevice, Guid.NewGuid().ToString());

        // Act
        var actualDevice = _dataStream.GetParentDevice();

        // Assert
        Assert.IsNotNull(actualDevice);
        Assert.AreSame(expectedDevice, actualDevice);
    }

    [TestMethod]
    public void GetParentDevice_IsNotGcDevice_ThrowsInvalidOperationException()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => _dataStream.GetParentDevice());
    }

    #endregion

    #region EventhandlerTests

    [TestMethod]
    public void OnNewBuffer_IsStreaming_BufferIsQueued()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(_testBuffer, DateTime.Now));

        // Assert
        Assert.AreEqual(1, _dataStream.AwaitDeliveryCount);
    }

    [TestMethod]
    public void OnNewBuffer_IsStreaming_BufferTransferredIsRaised()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();
        int eventCounter = 0;
        GcBuffer buffer = null;
        _dataStream.BufferTransferred += (s, e) => { eventCounter++; buffer = e.Buffer; };

        // Act
        _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(_testBuffer, DateTime.Now));

        // Assert
        Assert.AreEqual(1, eventCounter);
        Assert.IsNotNull(buffer);
        Assert.AreEqual(_testBuffer, buffer);
    }

    [TestMethod]
    public void OnNewBuffer_IsStreaming_StatisticsIsUpdated()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(_testBuffer, DateTime.Now));

        // Assert
        Assert.AreEqual(1, _dataStream.AwaitDeliveryCount);
        Assert.AreEqual<ulong>(1, _dataStream.DeliveredFrameCount);
        Assert.IsGreaterThan(0, _dataStream.FrameRate);
        Assert.IsGreaterThan(0, _dataStream.FrameRateAverage);
    }

    [TestMethod]
    public void OnNewBuffer_IsNotStreaming_NoDataIsDelivered()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act
        _mockDevice.Raise(s => s.NewBuffer += null, new NewBufferEventArgs(_testBuffer, DateTime.Now));

        // Assert
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
        Assert.AreEqual<ulong>(0, _dataStream.DeliveredFrameCount);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(3)]
    public void OnFailedBuffer_FailedFrameCountIsIncremented(int count)
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        for (int i = 0; i < count; i++)
            _mockDevice.Raise(s => s.FailedBuffer += null, EventArgs.Empty);

        // Assert
        Assert.AreEqual(count, (int)_dataStream.FailedFrameCount);
    }

    #endregion
}