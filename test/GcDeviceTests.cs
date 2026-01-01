using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests
{
    [TestClass]
    public class GcDeviceTests
    {
        // Only testing the non-abstract methods/properties here...

        #region Fields

        /// <summary>
        /// Mocked device to be instantiated for unit testing.
        /// </summary>
        private Mock<GcDevice> _device;

        #endregion

        #region TestConfiguration

        [TestCleanup]
        public void TestCleanUp()
        {
            _device.Reset();
        }

        #endregion

        #region ConstructorTests

        [TestMethod]
        public void GcDevice_ValidateInitialization()
        {
            // Act
            _device = new Mock<GcDevice>();

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
            _device = new Mock<GcDevice>();

            // Act
            var dataStream = _device.Object.OpenDataStream();

            // Assert
            Assert.IsNotNull(dataStream);
        }

        [TestMethod]
        public void OpenDataStream_NonExistingIDProvided_ReturnsNewStream()
        {
            // Arrange
            _device = new Mock<GcDevice>();

            // Act
            var dataStream = _device.Object.OpenDataStream("DataStream123");

            // Assert
            Assert.IsNotNull(dataStream);
        }

        [TestMethod]
        public void OpenDataStream_ExistingIDProvided_ReturnsSameStream()
        {
            // Arrange
            _device = new Mock<GcDevice>();
            var firstStream = _device.Object.OpenDataStream("DataStream123");

            // Act
            var secondStream = _device.Object.OpenDataStream(firstStream.StreamID);

            // Assert
            Assert.AreSame(firstStream, secondStream);
        }

        [TestMethod]
        public void GetNumDataStreams_NoDataStreamOpened_CountIsZero()
        {
            // Arrange
            _device = new Mock<GcDevice>();

            // Act
            uint count = _device.Object.GetNumDataStreams();

            Assert.AreEqual<uint>(0, count);
        }

        [TestMethod]
        public void GetNumDataStreams_MultipleDataStreamsOpened_ReturnsExpectedNumber()
        {
            // Arrange
            _device = new Mock<GcDevice>();
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
            _device = new Mock<GcDevice>();

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
            _device = new Mock<GcDevice>();
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
            _device = new Mock<GcDevice>();
            for (int i = 0; i < 3; i++)
                _device.Object.OpenDataStream();

            // Act/Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => _device.Object.GetDataStreamID(3));
        }

        [TestMethod]
        public void Close_GetNumDataStreams_ReturnsZero()
        {
            // Arrange
            _device = new Mock<GcDevice>();
            _device.Setup(d => d.Close()).CallBase();
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
            _device = new Mock<GcDevice> { CallBase = true };
            int eventCounter = 0;
            _device.Object.Closed += (s, e) => { eventCounter++; };

            // Act
            _device.Object.Close();

            // Assert
            Assert.AreEqual(1, eventCounter);
        }

        [TestMethod]
        public void FetchImage_ReturnsNull()
        {
            // Arrange
            _device = new Mock<GcDevice>();

            // Act
            var success = _device.Object.FetchImage(out GcBuffer buffer, 100);

            // Assert
            Assert.IsFalse(success);
            Assert.IsNull(buffer);
        }

        #endregion

        #region IEnumerableTests

        [TestMethod]
        public void IEnumerable_EnumerateDataStreams()
        {
            // Arrange
            _device = new Mock<GcDevice>();
            _device.Object.OpenDataStream();

            // Act/Assert
            foreach (var dataStream in _device.Object)
            {
                Assert.IsNotNull(dataStream);
            }
        }

        #endregion
    }
}