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
            Assert.IsTrue(_device.Object.PayloadSize == 0);
            Assert.IsNull(_device.Object.DeviceInfo);
            Assert.IsNull(_device.Object.Parameters);
            Assert.IsTrue(_device.Object.BufferCapacity == 0);
            Assert.IsFalse(_device.Object.IsAcquiring);
        }

        #endregion

        #region MethodTests

        [TestMethod]
        public void GetNumDataStreams_NoDataStreamOpened_CountIsZero()
        {
            // Arrange
            _device = new Mock<GcDevice>();

            // Act
            uint count = _device.Object.GetNumDataStreams();

            Assert.IsTrue(count == 0);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void GetNumDataStreams_MultipleDataStreamsOpened_ReturnsExpectedNumber(int expectedCount)
        {
            // Arrange
            _device = new Mock<GcDevice>();
            for (int i = 0; i < expectedCount; i++)
                _device.Object.OpenDataStream();

            // Act
            var actualCount = _device.Object.GetNumDataStreams();

            // Assert
            Assert.IsTrue(actualCount == expectedCount);
        }

        [TestMethod]
        public void GetDataStreamID_DataStreamOpenedWithID_ReturnsExpectedID()
        {
            // Arrange
            _device = new Mock<GcDevice>();
            var expectedID = "Datastream42";
            _device.Object.OpenDataStream(expectedID);

            // Act
            var actualID = _device.Object.GetDataStreamID(0);

            // Assert
            Assert.IsTrue(actualID == expectedID);
        }

        [TestMethod]
        public void GetDataStreamID_DataStreamOpenedWithoutID_ReturnsValidGuid()
        {
            // Arrange
            _device = new Mock<GcDevice>();
            _device.Object.OpenDataStream();

            // Act
            var ID = _device.Object.GetDataStreamID(0);

            // Assert
            Assert.IsTrue(Guid.TryParse(ID, out _));
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(3)]
        public void GetDataStreamID_IDOutofRange_ThrowsArgumentOutOfRangeException(int count)
        {
            // Arrange
            _device = new Mock<GcDevice>();
            for (int i = 0; i < count; i++)
                _device.Object.OpenDataStream();

            // Act/Assert
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => _device.Object.GetDataStreamID((uint)count));
        }

        [TestMethod]
        public void Close_GetNumDataStreamsReturnsZero()
        {
            // Arrange
            _device = new Mock<GcDevice>();
            _device.Setup(d => d.Close()).CallBase();
            _device.Object.OpenDataStream();

            // Act
            _device.Object.Close();

            // Assert
            Assert.IsTrue(_device.Object.GetNumDataStreams() == 0);
        }

        [TestMethod]
        public void Close_ClosedEventIsRaised()
        {
            // Arrange
            _device = new Mock<GcDevice> { CallBase = true };
            int eventCounter = 0;
            _device.Object.Closed += (s, e) => { eventCounter++; };

            // Act
            _device.Object.Close();

            // Assert
            Assert.IsTrue(eventCounter == 1);
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