using System;
using System.Collections.Generic;
using GcLib.UnitTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Library;

[TestClass]
[DoNotParallelize]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Category", "CA2263:RuleId", Justification = "Unit testing")]
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

        // Reset the failure mode of FailingCam to None after each test
        FailingCam.CurrentFailureMode = FailingCam.FailureMode.None;
    }

    #endregion

    #region ConstructorTests

    [TestMethod]
    public void GcLibrary_IsNotInitialized()
    {
        Assert.IsFalse(GcLibrary.IsInitialized);
    }

    #endregion

    #region InitTests

    [TestMethod]
    public void Init_IsNotInitialized_IsInitializedIsTrue()
    {
        // Act
        GcLibrary.Init(false);

        // Assert
        Assert.IsTrue(GcLibrary.IsInitialized);
    }

    [TestMethod]
    public void Init_IsInitialized_ThrowsInvalidOperationException()
    {
        // Arrange
        GcLibrary.Init(false);

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

    #endregion

    #region GetDeviceClassInfoTests

    [TestMethod]
    public void GetDeviceClassInfo_IsRegistered_ReturnsInfo()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act
        var classInfo = GcLibrary.GetDeviceClassInfo<VirtualCam>();

        // Assert
        Assert.AreEqual(typeof(VirtualCam), classInfo.DeviceType);
        Assert.AreEqual(nameof(VirtualCam), classInfo.Name);
    }

    [TestMethod]
    public void GetDeviceClassInfo_IsNotRegistered_ThrowsArgumentException()
    {
        // Act/Assert
        Assert.Throws<ArgumentException>(GcLibrary.GetDeviceClassInfo<VirtualCam>);
    }

    #endregion

    #region RegisterTests

    [TestMethod]
    public void RegisterGeneric_IsNotInitialized_ThrowsInvalidOperationException()
    {
        // Act/Assert
        Assert.Throws<InvalidOperationException>(GcLibrary.Register<VirtualCam>);
    }

    [TestMethod]
    public void RegisterGeneric_DeviceClassIsRegistered_ThrowsArgumentException()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act/Assert
        Assert.Throws<ArgumentException>(GcLibrary.Register<VirtualCam>);
    }

    [TestMethod]
    public void RegisterGeneric_DeviceClassIsNotRegistered_DeviceClassIsRegistered()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Register<VirtualCam>();

        // Assert
        Assert.Contains(VirtualCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void RegisterGeneric_MismatchedDeviceClassInfo_IsNotAddedToRegisteredDeviceClasses()
    {
        // Arrange
        GcLibrary.Init(false);
        FailingCam.CurrentFailureMode = FailingCam.FailureMode.DeviceClassInfoMismatch;

        // Act
        GcLibrary.Register<FailingCam>();

        // Assert
        Assert.IsFalse(GcLibrary.IsRegistered<FailingCam>());
    }

    [TestMethod]
    public void RegisterGeneric_ThrowsAtInitialization_IsNotAddedToRegisteredDeviceClasses()
    {
        // Arrange
        GcLibrary.Init(false);
        FailingCam.CurrentFailureMode = FailingCam.FailureMode.InitializationFailure;

        // Act
        GcLibrary.Register<FailingCam>();

        // Assert
        Assert.DoesNotContain(FailingCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void RegisterNonGeneric_IsNotInitialized_ThrowsInvalidOperationException()
    {
        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => GcLibrary.Register(typeof(VirtualCam)));
    }

    [TestMethod]
    public void RegisterNonGeneric_DeviceClassIsRegistered_ThrowsArgumentException()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register(typeof(VirtualCam));

        // Act/Assert
        Assert.Throws<ArgumentException>(() => GcLibrary.Register(typeof(VirtualCam)));
    }

    [TestMethod]
    public void RegisterNonGeneric_DeviceClassIsNotRegistered_DeviceClassIsRegistered()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Register(typeof(VirtualCam));

        // Assert
        Assert.Contains(VirtualCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void RegisterNonGeneric_ThrowsAtInitialization_IsNotAddedToRegisteredDeviceClasses()
    {
        // Arrange
        GcLibrary.Init(false);
        FailingCam.CurrentFailureMode = FailingCam.FailureMode.InitializationFailure;

        // Act
        GcLibrary.Register(typeof(FailingCam));

        // Assert
        Assert.DoesNotContain(FailingCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void RegisterNonGeneric_TypeIsNotDeviceClass_ThrowsArgumentException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act/Assert
        Assert.Throws<ArgumentException>(() => GcLibrary.Register(typeof(string)));
    }

    #endregion

    #region UnregisterTests

    [TestMethod]
    public void UnregisterGeneric_IsNotInitialized_ThrowsInvalidOperationException()
    {
        // Act/Assert
        Assert.Throws<InvalidOperationException>(GcLibrary.Unregister<VirtualCam>);
    }

    [TestMethod]
    public void UnregisterGeneric_DeviceClassIsRegistered_DeviceClassIsNotRegistered()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act
        GcLibrary.Unregister<VirtualCam>();

        // Assert
        Assert.IsFalse(GcLibrary.IsRegistered<VirtualCam>());
        Assert.DoesNotContain(VirtualCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void UnregisterGeneric_DeviceClassIsNotRegistered_ThrowsArgumentException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act/Assert
        Assert.Throws<ArgumentException>(GcLibrary.Unregister<VirtualCam>);

    }

    [TestMethod]
    public void UnregisterGeneric_ThrowsAtRelease_DeviceClassIsNotRegistered()
    {
        // Arrange
        GcLibrary.Init(false);
        FailingCam.CurrentFailureMode = FailingCam.FailureMode.ReleaseFailure;
        GcLibrary.Register<FailingCam>();

        // Act/Assert
        GcLibrary.Unregister<FailingCam>();

        // Assert
        Assert.IsFalse(GcLibrary.IsRegistered<FailingCam>());
        Assert.DoesNotContain(FailingCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void UnregisterNonGeneric_IsNotInitialized_ThrowsInvalidOperationException()
    {
        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => GcLibrary.Unregister(typeof(VirtualCam)));
    }

    [TestMethod]
    public void UnregisterNonGeneric_DeviceClassIsRegistered_DeviceClassIsNotRegistered()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register(typeof(VirtualCam));

        // Act
        GcLibrary.Unregister(typeof(VirtualCam));

        // Assert
        Assert.DoesNotContain(VirtualCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void UnregisterNonGeneric_DeviceClassIsNotRegistered_ThrowsArgumentException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act/Assert
        Assert.Throws<ArgumentException>(() => GcLibrary.Unregister(typeof(VirtualCam)));
    }

    [TestMethod]
    public void UnregisterNonGeneric_TypeIsNotDeviceClass_ThrowsArgumentException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act/Assert
        Assert.Throws<ArgumentException>(() => GcLibrary.Unregister(typeof(string)));
    }

    #endregion

    #region IsRegisteredTests

    [TestMethod]
    public void IsRegisteredGeneric_DeviceClassIsNotRegistered_ReturnsFalse()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        bool isRegistered = GcLibrary.IsRegistered<VirtualCam>();

        // Assert
        Assert.IsFalse(isRegistered);
    }

    [TestMethod]
    public void IsRegisteredGeneric_DeviceClassIsRegistered_ReturnsTrue()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act
        bool isRegistered = GcLibrary.IsRegistered<VirtualCam>();

        // Assert
        Assert.IsTrue(isRegistered);
    }

    [TestMethod]
    public void IsRegisteredNonGeneric_DeviceClassIsNotRegistered_ReturnsFalse()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        bool isRegistered = GcLibrary.IsRegistered(typeof(VirtualCam));

        // Assert
        Assert.IsFalse(isRegistered);
    }

    [TestMethod]
    public void IsRegisteredNonGeneric_DeviceClassIsRegistered_ReturnsTrue()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act
        bool isRegistered = GcLibrary.IsRegistered(typeof(VirtualCam));

        // Assert
        Assert.IsTrue(isRegistered);
    }

    [TestMethod]
    public void IsRegisteredNonGeneric_TypeIsNotDeviceClass_ReturnsFalse()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        bool isRegistered = GcLibrary.IsRegistered(typeof(string));

        // Assert
        Assert.IsFalse(isRegistered);
    }

    #endregion

    #region GetImplementedDeviceClassesTests

    [TestMethod]
    public void GetImplementedDeviceClasses_ReturnsNonEmpty()
    {
        var implementedDeviceClasses = GcLibrary.GetImplementedDeviceClasses();

        Assert.IsNotEmpty(implementedDeviceClasses);
        Assert.AreEqual(typeof(VirtualCam), implementedDeviceClasses[0]); // Ensure that VirtualCam (device class in the same assembly) is included and comes first in the list of implemented device classes
        Assert.Contains(typeof(FailingCam), implementedDeviceClasses); // Ensure that FailingCam (device class in an external assembly) is also included in the implemented device classes
    }

    [TestMethod]
    public void GetImplementedDeviceClasses_ExcludesNonDeviceTypes()
    {
        var implementedDeviceClasses = GcLibrary.GetImplementedDeviceClasses();

        Assert.DoesNotContain(typeof(GcDevice), implementedDeviceClasses); // Ensure that the base GcDevice class is not included in the implemented device classes
        Assert.DoesNotContain(typeof(GcDevice<>), implementedDeviceClasses); // Ensure that the generic GcDevice<T> class is not included in the implemented device classes
        Assert.DoesNotContain(typeof(MockableCam), implementedDeviceClasses); // Abstract device bases are not concrete device classes
        Assert.DoesNotContain(typeof(string), implementedDeviceClasses); // Ensure that a non-device type (e.g., string) is not included in the implemented device classes
    }

    #endregion

    #region GetRegisteredDeviceClassesTests

    [TestMethod]
    public void GetRegisteredDeviceClasses_IsInitialized_ReturnsNonEmpty()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act
        var types = GcLibrary.GetRegisteredDeviceClasses();

        // Assert
        Assert.IsNotEmpty(types);
    }

    [TestMethod]
    public void GetRegisteredDeviceClasses_IsNotInitialized_ReturnsEmpty()
    {
        // Act
        var types = GcLibrary.GetRegisteredDeviceClasses();

        // Assert
        Assert.IsEmpty(types);
    }

    #endregion

    #region CloseTests

    [TestMethod]
    public void Close_IsNotInitialized()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Close();

        // Assert
        Assert.IsFalse(GcLibrary.IsInitialized);
    }

    [TestMethod]
    public void Close_RegisteredDeviceClass_IsUnregistered()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();
        GcLibrary.Register<FailingCam>();

        // Act
        GcLibrary.Close();

        // Assert
        Assert.IsFalse(GcLibrary.IsInitialized);
        Assert.IsFalse(GcLibrary.IsRegistered<VirtualCam>());
        Assert.IsFalse(GcLibrary.IsRegistered<FailingCam>());
    }

    [TestMethod]
    public void Close_GetRegisteredDeviceClasses_ReturnsEmpty()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Close();

        // Assert
        Assert.IsEmpty(GcLibrary.GetRegisteredDeviceClasses());
    }

    #endregion

    #region InternalTests

    [TestMethod]
    public void GetDeviceFactory_DeviceTypeIsRegistered_ReturnsDelegate()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act
        var factory = GcLibrary.GetDeviceFactory(typeof(VirtualCam));

        // Assert
        Assert.IsNotNull(factory);
    }

    [TestMethod]
    public void GetDeviceFactory_DeviceTypeIsNotRegistered_ThrowsKeyNotFoundException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => GcLibrary.GetDeviceFactory(typeof(VirtualCam)));
    }

    [TestMethod]
    public void GetDeviceEnumerator_DeviceTypeIsRegistered_ReturnsDelegate()
    {
        // Arrange
        GcLibrary.Init(false);
        GcLibrary.Register<VirtualCam>();

        // Act
        var enumerator = GcLibrary.GetDeviceEnumerator(typeof(VirtualCam));

        // Assert
        Assert.IsNotNull(enumerator);
    }

    [TestMethod]
    public void GetDeviceEnumerator_DeviceTypeIsNotRegistered_ThrowsKeyNotFoundException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => GcLibrary.GetDeviceEnumerator(typeof(VirtualCam)));
    }

    #endregion
}