using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests
{
    [TestClass]
    public class GcLibraryTests
    {
        #region TestConfiguration

        [ClassInitialize]
        public static void ClassInitialize(TestContext _)
        {
            if (GcLibrary.IsInitialized)
                GcLibrary.Close();
        }

        [TestCleanup]
        public void TestCleanUp()
        {
            if (GcLibrary.IsInitialized)
                GcLibrary.Close();
        }

        #endregion

        #region ConstructorTests

        [TestMethod]
        public void GcLibrary_IsNotInitialized()
        {
            Assert.IsFalse(GcLibrary.IsInitialized);
        }

        #endregion

        #region MethodTests

        [TestMethod]
        public void Init_UnInitialized_IsInitializedIsTrue()
        {
            // Act
            GcLibrary.Init();

            // Assert
            Assert.IsTrue(GcLibrary.IsInitialized);
        }

        [TestMethod]
        public void Init_Initialized_ThrowsInvalidOperationException()
        {
            // Arrange
            GcLibrary.Init();

            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => GcLibrary.Init());
        }

        [TestMethod]
        public void Init_AutoRegister_DeviceClassesAreRegistered()
        {
            // Act
            GcLibrary.Init();

            // Assert
            Assert.IsNotEmpty(GcLibrary.GetRegisteredDeviceClasses());
        }

        [TestMethod]
        public void Init_ManualRegister_DeviceClassesAreNotRegistered()
        {
            // Act
            GcLibrary.Init(false);

            // Assert
            Assert.IsEmpty(GcLibrary.GetRegisteredDeviceClasses());
        }

        [TestMethod]
        public void GetDeviceClassInfo_ValidType_ReturnsInfo()
        {
            // Act
            var classInfo = GcLibrary.GetDeviceClassInfo<VirtualCam>();

            // Assert
            Assert.IsNotNull(classInfo);
            Assert.AreEqual(typeof(VirtualCam), classInfo.DeviceType);
        }

        [TestMethod]
        public void Register_DeviceClassIsNotRegistered_DeviceClassIsRegistered()
        {
            // Arrange
            GcLibrary.Init(false);

            // Act
            GcLibrary.Register<VirtualCam>();

            // Assert
            Assert.IsTrue(GcLibrary.GetAvailableDeviceClasses().Contains(VirtualCam.DeviceClassInfo));
            Assert.IsTrue(GcLibrary.GetRegisteredDeviceClasses().Contains(VirtualCam.DeviceClassInfo));
        }

        [TestMethod]
        public void Register_DeviceClassIsRegistered_ThrowsArgumentException()
        {
            // Arrange
            GcLibrary.Init();

            // Act/Assert
            Assert.Throws<ArgumentException>(GcLibrary.Register<VirtualCam>);
        }

        [TestMethod]
        public void Unregister_DeviceClassIsRegistered_DeviceClassIsNotRegistered()
        {
            // Arrange
            GcLibrary.Init();

            // Act
            GcLibrary.Unregister<VirtualCam>();

            // Assert
            Assert.IsFalse(GcLibrary.GetAvailableDeviceClasses().Contains(VirtualCam.DeviceClassInfo));
            Assert.IsFalse(GcLibrary.GetRegisteredDeviceClasses().Contains(VirtualCam.DeviceClassInfo));
        }

        [TestMethod]
        public void Unregister_DeviceClassIsNotRegistered_ThrowsArgumentException()
        {
            // Arrange
            GcLibrary.Init(false);

            // Act/Assert
            Assert.Throws<ArgumentException>(GcLibrary.Unregister<VirtualCam>);

        }

        [TestMethod]
        public void GetAvailableDeviceClasses_IsInitialized_ReturnsNonEmptyEnumerable()
        {
            // Arrange
            GcLibrary.Init();

            // Act
            var types = GcLibrary.GetAvailableDeviceClasses();

            // Assert
            Assert.IsNotEmpty(types);
        }

        [TestMethod]
        public void GetAvailableDeviceClasses_IsNotInitialized_ThrowsInvalidOperationException()
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(GcLibrary.GetAvailableDeviceClasses);
        }

        [TestMethod]
        public void GetRegisteredDeviceClasses_IsInitialized_ReturnsNonEmptyEnumerable()
        {
            // Arrange
            GcLibrary.Init();

            // Act
            var types = GcLibrary.GetRegisteredDeviceClasses();

            // Assert
            Assert.IsNotEmpty(types);
        }

        [TestMethod]
        public void GetRegisteredDeviceClasses_IsNotInitialized_ReturnsEmptyEnumerable()
        {
            // Act
            var types = GcLibrary.GetRegisteredDeviceClasses();

            // Assert
            Assert.IsEmpty(types);
        }

        [TestMethod]
        public void Close_IsNotInitialized()
        {
            // Arrange
            GcLibrary.Init();

            // Act
            GcLibrary.Close();

            // Assert
            Assert.IsFalse(GcLibrary.IsInitialized);
        }

        [TestMethod]
        public void Close_GetAvailableDeviceClasses_ThrowsInvalidOperationException()
        {
            // Arrange
            GcLibrary.Init();

            // Act
            GcLibrary.Close();

            // Assert
            Assert.Throws<InvalidOperationException>(GcLibrary.GetAvailableDeviceClasses);
        }

        [TestMethod]
        public void Close_GetRegisteredDeviceClasses_CountIsZero()
        {
            // Arrange
            GcLibrary.Init();

            // Act
            GcLibrary.Close();

            // Assert
            Assert.IsEmpty(GcLibrary.GetRegisteredDeviceClasses());
        }

        #endregion
    }
}