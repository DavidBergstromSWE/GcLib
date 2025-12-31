using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace GcLib.UnitTests
{
    [TestClass]
    [DoNotParallelize]
    public class GcSystemTests
    {
        #region Fields

        private GcSystem _system;
        private GcDevice _device;

        #endregion

        #region TestConfiguration

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            GcLibrary.Init(false);
            GcLibrary.Register<VirtualCam>();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            GcLibrary.Close();
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            _device?.Close();
            _system?.Dispose();
        }

        #endregion

        #region ConstructorTests

        [TestMethod]
        public void GcSystem_ValidateInitialization()
        {
            // Act
            _system = new GcSystem();

            // Assert
            Assert.IsNotNull(_system);
            Assert.AreEqual<uint>(0, _system.GetNumDevices());
            Assert.IsEmpty(_system.GetDeviceList());
        }

        [TestMethod]
        public void GcSystem_SecondInstantiation_ThrowsInvalidOperationException()
        {
            // Arrange
            _system = new GcSystem();

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => new GcSystem());
        }

        #endregion

        #region MethodTests

        [TestMethod]
        public void UpdateDeviceList_DeviceListIsChanged()
        {
            // Arrange
            _system = new GcSystem();

            // Act
            bool changed = _system.UpdateDeviceList();

            // Assert
            Assert.IsTrue(changed);
        }

        [TestMethod]
        public void UpdateDeviceList_SecondTime_DeviceListIsNotChanged()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();

            // Act
            bool changed = _system.UpdateDeviceList();

            // Assert
            Assert.IsFalse(changed);
        }

        [TestMethod]
        public void GetDeviceList_FromUpdatedDeviceList_DeviceListIsNotEmpty()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();

            // Act
            var devices= _system.GetDeviceList();

            // Assert
            Assert.IsNotEmpty(devices);
        }

        [TestMethod]
        public void GetNumDevices_FromUpdatedDeviceList_CountGreaterThanZero()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();

            // Act
            uint count = _system.GetNumDevices();

            // Assert
            Assert.IsGreaterThan<uint>(0, count);
        }

        [TestMethod]
        public void OpenDevice_DeviceIsAvailable_DeviceIsOpenAndNotNull()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var devices = _system.GetDeviceList();

            // Act
            _device = _system.OpenDevice(devices[0]);

            // Assert
            Assert.IsTrue(devices[0].IsOpen);
            Assert.IsFalse(devices[0].IsAccessible);
            Assert.IsNotNull(_device);
        }

        [TestMethod]
        public void OpenDevice_DeviceIsNotAvailable_ThrowsArgumentException()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();

            // Act/Assert
            Assert.Throws<ArgumentException>(() => _system.OpenDevice("FakeID"));
        }


        [TestMethod]
        public void OpenDevice_DeviceIsNotAccessible_ThrowsInvalidOperationException()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var devices = _system.GetDeviceList();
            _system.OpenDevice(devices[0]);

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => _system.OpenDevice(devices[0]));
        }

        [TestMethod]
        public void CloseDevice_DeviceListDoesNotContainOpenDevice()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var devices = _system.GetDeviceList();
            _device = _system.OpenDevice(devices[0]);

            // Act
            _device.Close();

            // Assert
            Assert.IsFalse(devices.Find(d => d.UniqueID == _device.DeviceInfo.UniqueID).IsOpen);
        }

        [TestMethod]
        public void GetDeviceID_DeviceIsAvailable_ReturnsID()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var devices = _system.GetDeviceList();
            var expectedID = devices[0].UniqueID;

            // Act
            var actualID = _system.GetDeviceID(0);

            // Assert
            Assert.AreEqual(expectedID, actualID);
        }

        [TestMethod]
        public void GetDeviceID_DeviceIsNotAvailable_ReturnsNull()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var count = _system.GetNumDevices();

            // Act
            var actualID = _system.GetDeviceID(count + 1);

            // Assert
            Assert.IsNull(actualID);
        }

        [TestMethod]
        public void GetDeviceInfo_DeviceIsAvailable_ReturnsInfo()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var devices = _system.GetDeviceList();
            var expectedDeviceInfo = devices[0];

            // Act
            var actualDeviceInfo = _system.GetDeviceInfo(devices[0].UniqueID);

            // Assert
            Assert.AreEqual(expectedDeviceInfo, actualDeviceInfo);
        }

        [TestMethod]
        public void GetDeviceInfo_DeviceIsNotAvailable_ReturnsNull()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();

            // Act
            var actualDeviceInfo = _system.GetDeviceInfo("FakeID");

            // Assert
            Assert.IsNull(actualDeviceInfo);
        }

        #endregion

        #region IDisposableTests

        [TestMethod]
        public void Dispose_AllDevicesAreClosed()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var devices = _system.GetDeviceList();
            _device = _system.OpenDevice(devices[0]);

            // Act
            _system.Dispose();

            // Assert
            Assert.IsFalse(devices.Any(d => d.IsOpen));
        }

        [TestMethod]
        public void Dispose_NoDevicesAreAvailable()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();
            var devices = _system.GetDeviceList();
            _device = _system.OpenDevice(devices[0]);

            // Act
            _system.Dispose();

            // Assert
            Assert.AreEqual<uint>(0, _system.GetNumDevices());
            Assert.IsEmpty(_system.GetDeviceList());
        }

        [TestMethod]
        public void Dispose_CanBeInstantiatedAgain()
        {
            // Arrange
            _system = new GcSystem();

            // Act
            _system.Dispose();

            // Assert
            _ = new GcSystem();
        }

        #endregion

        #region IEnumerableTests

        [TestMethod]
        public void IEnumerable_EnumerateDeviceInfos()
        {
            // Arrange
            _system = new GcSystem();
            _system.UpdateDeviceList();

            // Act/Assert
            foreach (var deviceInfo in _system)
            {
                Assert.IsNotNull(deviceInfo);
            }
        }

        #endregion
    }
}