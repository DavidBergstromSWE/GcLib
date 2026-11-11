using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
    /// Registered stream sink callbacks for simulated producer dispatch.
    /// </summary>
    private readonly Dictionary<string, (Action<object, NewBufferEventArgs> OnNewBuffer, Action<object, EventArgs> OnFailedBuffer)> _registeredSinks = [];

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
        _mockDevice.Setup(d => d.RegisterBufferSink(It.IsAny<string>(), It.IsAny<Action<object, NewBufferEventArgs>>(), It.IsAny<Action<object, EventArgs>>()))
                   .Returns<string, Action<object, NewBufferEventArgs>, Action<object, EventArgs>>((streamId, onNewBuffer, onFailedBuffer) =>
                   {
                       _registeredSinks[streamId] = (onNewBuffer, onFailedBuffer);
                       return new SinkRegistration(() => _registeredSinks.Remove(streamId));
                   });

        // Generate simple buffer data for testing.
        _testBuffer = FakeBufferProvider.GetFakeBuffer(3, 3, PixelFormat.Mono8);
    }

    [TestCleanup]
    public void TestCleanUp()
    {
        _dataStream.Stop();
        _dataStream.Close();
        _dataStream = null;

        _registeredSinks.Clear();
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

        Assert.IsFalse(_dataStream.IsStreaming);
        Assert.IsTrue(_dataStream.IsOpen);
    }

    [TestMethod]
    public void Constructor_Asynchronous_DeliveryModeIsSet()
    {
        // Arrange/Act
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString(), deliveryMode: AcquisitionDeliveryMode.Asynchronous);

        // Assert
        Assert.AreEqual(AcquisitionDeliveryMode.Asynchronous, _dataStream.DeliveryMode);
    }

    #endregion

    #region MethodTests

    [TestMethod]
    public void RetrieveImage_QueueIsEmpty_BufferIsNull()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act
        var success = _dataStream.RetrieveBuffer(out GcBuffer buffer, 0);

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
        RaiseNewBuffer();

        // Act
        var success = _dataStream.RetrieveBuffer(out GcBuffer buffer, 0);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(buffer);
        Assert.AreEqual(_testBuffer.Width, buffer.Width);
        Assert.AreEqual(_testBuffer.Height, buffer.Height);
        Assert.AreEqual(_testBuffer.PixelFormat, buffer.PixelFormat);
        Assert.AreEqual(_testBuffer.FrameID, buffer.FrameID);
        Assert.AreEqual(_testBuffer.TimeStamp, buffer.TimeStamp);
        Assert.AreSequenceEqual(_testBuffer.ImageData, buffer.ImageData);
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
    }

    [TestMethod]
    public void RetrieveImage_Asynchronous_ReturnsFalse()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString(), deliveryMode: AcquisitionDeliveryMode.Asynchronous);
        _dataStream.Start();
        RaiseNewBuffer();

        // Act
        var success = _dataStream.RetrieveBuffer(out GcBuffer buffer, 0);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(buffer);
    }

    [TestMethod]
    public void RetrieveImage_NoBufferWithinTimeout_ReturnsFalseAfterTimeout()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        const int timeout = 50;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var success = _dataStream.RetrieveBuffer(out GcBuffer buffer, timeout);
        stopwatch.Stop();

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(buffer);
        Assert.IsGreaterThanOrEqualTo(timeout - 5, stopwatch.ElapsedMilliseconds); // Allowing a small margin for timing inaccuracies
    }

    [TestMethod]
    public void RetrieveImage_QueueHasMultipleBuffers_BuffersAreRetrievedInOrder()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        var firstBuffer = FakeBufferProvider.GetFakeBuffer();
        var secondBuffer = FakeBufferProvider.GetFakeBuffer();

        RaiseNewBuffer(CreateNewBufferEventArgs(firstBuffer, _dataStream.StreamID));
        RaiseNewBuffer(CreateNewBufferEventArgs(secondBuffer, _dataStream.StreamID));

        // Act
        var firstSuccess = _dataStream.RetrieveBuffer(out GcBuffer retrievedFirstBuffer, 0);
        var secondSuccess = _dataStream.RetrieveBuffer(out GcBuffer retrievedSecondBuffer, 0);

        // Assert
        Assert.IsTrue(firstSuccess);
        Assert.IsTrue(secondSuccess);
        Assert.AreEqual(firstBuffer.Width, retrievedFirstBuffer.Width);
        Assert.AreEqual(firstBuffer.Height, retrievedFirstBuffer.Height);
        Assert.AreEqual(firstBuffer.PixelFormat, retrievedFirstBuffer.PixelFormat);
        Assert.AreEqual(firstBuffer.FrameID, retrievedFirstBuffer.FrameID);
        Assert.AreEqual(firstBuffer.TimeStamp, retrievedFirstBuffer.TimeStamp);
        Assert.AreSequenceEqual(firstBuffer.ImageData, retrievedFirstBuffer.ImageData);
        Assert.AreEqual(secondBuffer.Width, retrievedSecondBuffer.Width);
        Assert.AreEqual(secondBuffer.Height, retrievedSecondBuffer.Height);
        Assert.AreEqual(secondBuffer.PixelFormat, retrievedSecondBuffer.PixelFormat);
        Assert.AreEqual(secondBuffer.FrameID, retrievedSecondBuffer.FrameID);
        Assert.AreEqual(secondBuffer.TimeStamp, retrievedSecondBuffer.TimeStamp);
        Assert.AreSequenceEqual(secondBuffer.ImageData, retrievedSecondBuffer.ImageData);
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
    public void Start_StartAcquisitionThrows_StateIsRolledBack()
    {
        // Arrange
        _mockDevice.Setup(d => d.StartAcquisition()).Throws(new InvalidOperationException("Start failure"));
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => _dataStream.Start());
        Assert.IsFalse(_dataStream.IsStreaming);

        // Verify that NewBuffer handler was detached as part of rollback.
        RaiseNewBuffer();
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
        Assert.AreEqual<ulong>(0, _dataStream.DeliveredFrameCount);
    }

    [TestMethod]
    public void Start_StartAcquisitionThrowsOnce_SecondStartSucceeds()
    {
        // Arrange
        bool shouldThrow = true;
        _mockDevice.Setup(d => d.StartAcquisition()).Callback(() =>
        {
            if (shouldThrow)
            {
                shouldThrow = false;
                throw new InvalidOperationException("Start failure");
            }
        });

        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());

        // Act
        Assert.Throws<InvalidOperationException>(() => _dataStream.Start());
        _dataStream.Start();

        // Assert
        Assert.IsTrue(_dataStream.IsStreaming);
        RaiseNewBuffer();
        Assert.AreEqual(1, _dataStream.AwaitDeliveryCount);
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
            RaiseNewBuffer();

        // Assert
        Assert.IsFalse(_dataStream.IsStreaming);
        Assert.AreEqual(1, eventCounter);
    }

    [TestMethod]
    public void OnNewBuffer_DirectPayload_BufferUsesProducerOwnedData()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        RaiseNewBuffer();
        var success = _dataStream.RetrieveBuffer(out GcBuffer buffer, 0);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(buffer);
        Assert.AreSame(_testBuffer.ImageData, buffer.ImageData);
    }

    [TestMethod]
    public void OnNewBuffer_DirectPayloadClone_BufferUsesProvidedData()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        var payload = (byte[])_testBuffer.ImageData.Clone();
        var eventArgs = new NewBufferEventArgs(_dataStream.StreamID,
                                               DateTime.Now,
                                               new GcBuffer(payload,
                                                            _testBuffer.Width,
                                                            _testBuffer.Height,
                                                            _testBuffer.PixelFormat,
                                                            _testBuffer.FrameID,
                                                            _testBuffer.TimeStamp));

        // Act
        RaiseNewBuffer(eventArgs);
        var success = _dataStream.RetrieveBuffer(out GcBuffer buffer, 0);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(buffer);
        Assert.AreSame(payload, buffer.ImageData);
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(3)]
    [DataRow(5)]
    public void Start_AcquireMultipleImages_BuffersAreDeliveredAndQueued(int count)
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString(), outputQueueCapacity: count);

        // Act
        _dataStream.Start((ulong)count);
        for (int i = 0; i < count; i++)
            RaiseNewBuffer();

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
        RaiseNewBuffer();

        // Assert
        Assert.AreEqual(1, _dataStream.AwaitDeliveryCount);
    }

    [TestMethod]
    public void OnNewBuffer_StreamIDMismatch_BufferIsIgnored()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        RaiseNewBuffer(CreateNewBufferEventArgs(_testBuffer, Guid.NewGuid().ToString()));

        // Assert
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
        Assert.AreEqual<ulong>(0, _dataStream.DeliveredFrameCount);
    }

    [TestMethod]
    public void OnNewBuffer_EmptyStreamID_BufferIsAccepted()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        RaiseNewBuffer(CreateNewBufferEventArgs(_testBuffer, string.Empty));

        // Assert
        Assert.AreEqual(1, _dataStream.AwaitDeliveryCount);
        Assert.AreEqual<ulong>(1, _dataStream.DeliveredFrameCount);
    }

    [TestMethod]
    public void OnNewBuffer_Asynchronous_BufferTransferredIsRaisedAndNotQueued()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString(), deliveryMode: AcquisitionDeliveryMode.Asynchronous);
        _dataStream.Start();
        using var signal = new ManualResetEventSlim(false);
        int eventCounter = 0;
        GcBuffer buffer = null;
        _dataStream.BufferTransferred += (s, e) =>
        {
            eventCounter++;
            buffer = e.Buffer;
            signal.Set();
        };

        // Act
        RaiseNewBuffer();

        // Assert
        Assert.IsTrue(signal.Wait(500));
        Assert.AreEqual(1, eventCounter);
        Assert.IsNotNull(buffer);
        Assert.AreEqual(_testBuffer.Width, buffer.Width);
        Assert.AreEqual(_testBuffer.Height, buffer.Height);
        Assert.AreEqual(_testBuffer.PixelFormat, buffer.PixelFormat);
        Assert.AreEqual(_testBuffer.FrameID, buffer.FrameID);
        Assert.AreEqual(_testBuffer.TimeStamp, buffer.TimeStamp);
        Assert.AreSequenceEqual(_testBuffer.ImageData, buffer.ImageData);
        Assert.AreEqual(0, _dataStream.AwaitDeliveryCount);
    }

    [TestMethod]
    public void OnNewBuffer_IsStreaming_StatisticsIsUpdated()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();

        // Act
        RaiseNewBuffer();

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
        RaiseNewBuffer();

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
            RaiseFailedBuffer();

        // Assert
        Assert.AreEqual(count, (int)_dataStream.FailedFrameCount);
    }

    [TestMethod]
    public void OnNewBuffer_FrameGapExists_LostFrameCountIsIncrementedAndFrameDroppedRaised()
    {
        // Arrange
        _dataStream = new GcDataStream(_mockDevice.Object, Guid.NewGuid().ToString());
        _dataStream.Start();
        int frameDroppedEventCount = 0;
        _dataStream.BufferDropped += (s, e) => { frameDroppedEventCount++; };

        var firstBuffer = FakeBufferProvider.GetFakeBuffer(3, 3, PixelFormat.Mono8, 1, 100);
        var secondBuffer = FakeBufferProvider.GetFakeBuffer(3, 3, PixelFormat.Mono8, 3, 200);

        // Act
        RaiseNewBuffer(CreateNewBufferEventArgs(firstBuffer, _dataStream.StreamID));
        RaiseNewBuffer(CreateNewBufferEventArgs(secondBuffer, _dataStream.StreamID));

        // Assert
        Assert.AreEqual<ulong>(1, _dataStream.LostFrameCount);
        Assert.AreEqual(1, frameDroppedEventCount);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Creates stream-transfer event arguments from a <see cref="GcBuffer"/>.
    /// </summary>
    private static NewBufferEventArgs CreateNewBufferEventArgs(GcBuffer buffer, string streamID)
    {
        return new NewBufferEventArgs(streamID,
                                      DateTime.Now,
                                      buffer);
    }

    /// <summary>
    /// Raises an event indicating that a new buffer is available.
    /// </summary>
    private void RaiseNewBuffer()
    {
        if (_dataStream == null)
            return;

        RaiseNewBuffer(CreateNewBufferEventArgs(_testBuffer, _dataStream.StreamID));
    }

    /// <summary>
    /// Raises the new-buffer event for each registered sink.
    /// </summary>
    /// <param name="eventArgs">The event data supplied to each registered sink.</param>
    private void RaiseNewBuffer(NewBufferEventArgs eventArgs)
    {
        foreach (var (OnNewBuffer, OnFailedBuffer) in _registeredSinks.Values)
            OnNewBuffer(_mockDevice.Object, eventArgs);
    }
    
    /// <summary>
    /// Raises the failed-buffer event for each registered sink.
    /// </summary>
    private void RaiseFailedBuffer()
    {
        foreach (var (OnNewBuffer, OnFailedBuffer) in _registeredSinks.Values)
            OnFailedBuffer(_mockDevice.Object, EventArgs.Empty);
    }

    /// <summary>
    /// Represents a registration that invokes its disposal action at most once.
    /// </summary>
    /// <param name="disposeAction">The action to invoke when the registration is disposed.</param>
    private sealed class SinkRegistration(Action disposeAction) : IDisposable
    {
        private Action _disposeAction = disposeAction;

        public void Dispose()
        {
            Interlocked.Exchange(ref _disposeAction, null)?.Invoke();
        }
    }

    #endregion
}
