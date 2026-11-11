using System;
using GcLib.UnitTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests.Modules;

[TestClass]
public class GcDeviceTests
{
    // Only testing the non-abstract methods/properties here...

    #region Fields

    /// <summary>
    /// Mocked device to be instantiated for unit testing.
    /// </summary>
    private Mock<MockableCam> _device;

    #endregion

    #region TestConfiguration

    [TestCleanup]
    public void TestCleanUp()
    {
        _device?.Reset();
    }

    #endregion

    #region ConstructorTests

    [TestMethod]
    public void GcDevice_ValidateInitialization()
    {
        // Act
        _device = new Mock<MockableCam>();

        // Assert
        Assert.AreEqual<uint>(0, _device.Object.PayloadSize);
        Assert.IsNull(_device.Object.DeviceInfo);
        Assert.IsNull(_device.Object.Parameters);
        Assert.AreEqual<uint>(0, _device.Object.BufferCapacity);
        Assert.IsFalse(_device.Object.IsAcquiring);
    }

    #endregion

    #region MethodTests

    [TestMethod]
    public void OpenDataStream_NoIDProvided_ReturnsNewStream()
    {
        // Arrange
        _device = new Mock<MockableCam>();

        // Act
        var dataStream = _device.Object.OpenDataStream();

        // Assert
        Assert.IsNotNull(dataStream);
    }

    [TestMethod]
    public void OpenDataStream_NonExistingIDProvided_ReturnsNewStream()
    {
        // Arrange
        _device = new Mock<MockableCam>();

        // Act
        var dataStream = _device.Object.OpenDataStream("DataStream123");

        // Assert
        Assert.IsNotNull(dataStream);
    }

    [TestMethod]
    public void OpenDataStream_ExistingIDProvided_ReturnsSameStream()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        var firstStream = _device.Object.OpenDataStream("DataStream123");

        // Act
        var secondStream = _device.Object.OpenDataStream(firstStream.StreamID);

        // Assert
        Assert.AreSame(firstStream, secondStream);
    }

    [TestMethod]
    public void OpenDataStream_DeliveryModeSpecified_StreamUsesExpectedDeliveryMode()
    {
        // Arrange
        _device = new Mock<MockableCam>();

        // Act
        var dataStream = _device.Object.OpenDataStream(deliveryMode: AcquisitionDeliveryMode.Asynchronous);

        // Assert
        Assert.AreEqual(AcquisitionDeliveryMode.Asynchronous, dataStream.DeliveryMode);
    }

    [TestMethod]
    public void GetNumDataStreams_NoDataStreamOpened_CountIsZero()
    {
        // Arrange
        _device = new Mock<MockableCam>();

        // Act
        uint count = _device.Object.GetNumDataStreams();

        Assert.AreEqual<uint>(0, count);
    }

    [TestMethod]
    public void GetNumDataStreams_MultipleDataStreamsOpened_ReturnsExpectedNumber()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        for (int i = 0; i < 3; i++)
            _device.Object.OpenDataStream();

        // Act
        var actualCount = _device.Object.GetNumDataStreams();

        // Assert
        Assert.AreEqual<uint>(3, actualCount);
    }

    [TestMethod]
    public void GetDataStreamID_NoIDProvided_ReturnsValidGuidAsID()
    {
        // Arrange
        _device = new Mock<MockableCam>();

        // Act
        _device.Object.OpenDataStream();
        var dataStreamID = _device.Object.GetDataStreamID(0);

        // Assert
        Assert.IsTrue(Guid.TryParse(dataStreamID, out _));
    }

    [TestMethod]
    public void GetDataStreamID_ValidID_ReturnsExpectedID()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        var expectedID = "Stream1";
        _device.Object.OpenDataStream(expectedID);

        // Act/Assert
        var actualID = _device.Object.GetDataStreamID(0);
        Assert.AreEqual(expectedID, actualID);
    }

    [TestMethod]
    public void GetDataStreamID_IDOutofRange_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        for (int i = 0; i < 3; i++)
            _device.Object.OpenDataStream();

        // Act/Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _device.Object.GetDataStreamID(3));
    }

    [TestMethod]
    public void GetDataStreamID_NoDataStreamsOpened_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _device = new Mock<MockableCam>();

        // Act/Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _device.Object.GetDataStreamID(0));
    }

    [TestMethod]
    public void Close_GetNumDataStreams_ReturnsZero()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        _device.Object.OpenDataStream();

        // Act
        _device.Object.Close();

        // Assert
        Assert.AreEqual<uint>(0, _device.Object.GetNumDataStreams());
    }

    [TestMethod]
    public void Close_ClosedEvent_IsRaised()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        int eventCounter = 0;
        _device.Object.Closed += (s, e) => { eventCounter++; };

        // Act
        _device.Object.Close();

        // Assert
        Assert.AreEqual(1, eventCounter);
    }

    [TestMethod]
    public void Close_OpenDataStream_DataStreamIsClosed()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        var dataStream = _device.Object.OpenDataStream();

        // Act
        _device.Object.Close();

        // Assert
        Assert.IsFalse(dataStream.IsOpen);
    }

    [TestMethod]
    public void Close_DeviceInfoExists_UpdatesDeviceInfoFlags()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        var deviceInfo = new GcDeviceInfo("vendor", "model", "serial", "id", MockableCam.DeviceClassInfo, isAccessible: false, isOpen: true);
        typeof(GcDevice).GetProperty(nameof(GcDevice.DeviceInfo))?.SetValue(_device.Object, deviceInfo);

        // Act
        _device.Object.Close();

        // Assert
        Assert.IsFalse(_device.Object.DeviceInfo.IsOpen);
        Assert.IsTrue(_device.Object.DeviceInfo.IsAccessible);
    }

    [TestMethod]
    public void StartMultipleStreams_StartsDeviceOnceAndStopsAfterLastStream()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        _device.SetupGet(d => d.MaxConcurrentStreams).Returns(2);
        var firstStream = _device.Object.OpenDataStream("Stream1");
        var secondStream = _device.Object.OpenDataStream("Stream2");

        // Act
        firstStream.Start();
        secondStream.Start();
        firstStream.Stop();

        // Assert
        _device.Verify(d => d.StartAcquisition(), Times.Once);
        _device.Verify(d => d.StopAcquisition(), Times.Never);

        // Act
        secondStream.Stop();

        // Assert
        _device.Verify(d => d.StopAcquisition(), Times.Once);
    }

    [TestMethod]
    public void Start_StreamingStartedHandlerThrows_AcquisitionIsStoppedAndStreamSlotReleased()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        _device.SetupGet(d => d.MaxConcurrentStreams).Returns(1);
        var firstStream = _device.Object.OpenDataStream("Stream1");
        bool throwOnFirstStart = true;
        firstStream.StreamingStarted += (s, e) =>
        {
            if (throwOnFirstStart)
            {
                throwOnFirstStart = false;
                throw new InvalidOperationException("Streaming-started handler failure.");
            }
        };

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => firstStream.Start());
        _device.Verify(d => d.StartAcquisition(), Times.Once);
        _device.Verify(d => d.StopAcquisition(), Times.Once);
        Assert.IsFalse(firstStream.IsStreaming);

        firstStream.Start();
        Assert.IsTrue(firstStream.IsStreaming);
        firstStream.Stop();
    }

    [TestMethod]
    public void StartConcurrentStreams_ExceedingLimitThrows()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        _device.SetupGet(d => d.MaxConcurrentStreams).Returns(2);
        var firstStream = _device.Object.OpenDataStream("Stream1");
        var secondStream = _device.Object.OpenDataStream("Stream2");
        var thirdStream = _device.Object.OpenDataStream("Stream3");

        // Act
        firstStream.Start();
        secondStream.Start();

        // Assert
        Assert.Throws<InvalidOperationException>(() => thirdStream.Start());
        _device.Verify(d => d.StartAcquisition(), Times.Once);
        _device.Verify(d => d.StopAcquisition(), Times.Never);

        // Cleanup
        firstStream.Stop();
        secondStream.Stop();
        _device.Verify(d => d.StopAcquisition(), Times.Once);
    }

    [TestMethod]
    public void StartAcquisitionFailure_DoesNotReserveStreamSlot()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        int startAttempts = 0;
        _device.Setup(d => d.StartAcquisition()).Callback(() =>
        {
            if (++startAttempts == 1)
                throw new InvalidOperationException("Simulated acquisition start failure.");
        });
        var firstStream = _device.Object.OpenDataStream("Stream1");
        var secondStream = _device.Object.OpenDataStream("Stream2");

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => firstStream.Start());
        secondStream.Start();
        _device.Verify(d => d.StartAcquisition(), Times.Exactly(2));

        // Cleanup
        secondStream.Stop();
    }

    [TestMethod]
    public void StopAcquisitionFailure_DoesNotKeepStaleActiveStreamSlot()
    {
        // Arrange
        _device = new Mock<MockableCam> { CallBase = true };
        _device.SetupGet(d => d.MaxConcurrentStreams).Returns(1);

        int stopAttempts = 0;
        _device.Setup(d => d.StopAcquisition()).Callback(() =>
        {
            if (++stopAttempts == 1)
                throw new InvalidOperationException("Simulated acquisition stop failure.");
        });

        var firstStream = _device.Object.OpenDataStream("Stream1");
        var secondStream = _device.Object.OpenDataStream("Stream2");

        firstStream.Start();

        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => firstStream.Stop());
        secondStream.Start();

        // Cleanup
        secondStream.Stop();
    }

    [TestMethod]
    public void FetchImage_ReturnsNull()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        _device.SetupGet(d => d.MaxConcurrentStreams).Returns(1);

        // Act
        var success = _device.Object.FetchImage(out GcBuffer buffer, 100);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(buffer);
    }

    [TestMethod]
    public void FetchImage_NewBufferRaised_ReturnsTrueAndBuffer()
    {
        // Arrange
        var expectedBuffer = FakeBufferProvider.GetFakeBuffer();
        var device = new Mock<MockableCam>() { CallBase = true }; // Use CallBase to allow base method calls for FetchImage.
        device.Setup(d => d.StartAcquisition()).Callback(() =>
        {
            // Simulate acquisition started and raise the NewBuffer event.
            var onNewBuffer = typeof(MockableCam).BaseType?.GetMethod("OnNewBuffer", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic); // Access the protected OnNewBuffer method using reflection.
            var eventArgs = new NewBufferEventArgs(string.Empty,
                                                   DateTime.Now,
                                                   expectedBuffer);
            onNewBuffer?.Invoke(device.Object, [eventArgs]);
        });

        // Act
        var success = device.Object.FetchImage(out GcBuffer buffer, 250);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(buffer);
        Assert.AreEqual(expectedBuffer.Width, buffer.Width);
        Assert.AreEqual(expectedBuffer.Height, buffer.Height);
        Assert.AreEqual(expectedBuffer.PixelFormat, buffer.PixelFormat);
        Assert.AreEqual(expectedBuffer.FrameID, buffer.FrameID);
        Assert.AreEqual(expectedBuffer.TimeStamp, buffer.TimeStamp);
        Assert.AreSequenceEqual(expectedBuffer.ImageData, buffer.ImageData);
    }

    [TestMethod]
    public void FetchImage_Always_InvokesStartAndStopAcquisitionOnce()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        _device.SetupGet(d => d.MaxConcurrentStreams).Returns(1);

        // Act
        _ = _device.Object.FetchImage(out _, 10);

        // Assert
        _device.Verify(d => d.StartAcquisition(), Times.Once);
        _device.Verify(d => d.StopAcquisition(), Times.Once);
    }

    #endregion

    #region IEnumerableTests

    [TestMethod]
    public void IEnumerable_EnumerateDataStreams()
    {
        // Arrange
        _device = new Mock<MockableCam>();
        _device.Object.OpenDataStream();

        // Act/Assert
        foreach (var dataStream in _device.Object)
        {
            Assert.IsNotNull(dataStream);
        }
    }

    #endregion
}