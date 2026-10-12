using System;
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
    public void Init_UnInitialized_IsInitializedIsTrue()
    {
        // Act
        GcLibrary.Init(false);

        // Assert
        Assert.IsTrue(GcLibrary.IsInitialized);
    }

    [TestMethod]
    public void Init_Initialized_ThrowsInvalidOperationException()
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
    public void GetDeviceClassInfo_ValidType_ReturnsInfo()
    {
        // Act
        var classInfo = GcLibrary.GetDeviceClassInfo<VirtualCam>();

        // Assert
        Assert.AreEqual(typeof(VirtualCam), classInfo.DeviceType);
        Assert.AreEqual(nameof(VirtualCam), classInfo.Name);
    }

    #endregion

    #region RegisterTests

    [TestMethod]
    public void RegisterGeneric_IsNotInitialized_ThrowsInvalidOperationException()
    {
        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => GcLibrary.Register<VirtualCam>());
    }

    [TestMethod]
    public void RegisterGeneric_DeviceClassIsNotRegistered_DeviceClassIsRegistered()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Register<VirtualCam>();

        // Assert
        Assert.Contains(VirtualCam.DeviceClassInfo, GcLibrary.GetAvailableDeviceClasses());
        Assert.Contains(VirtualCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
    }

    [TestMethod]
    public void RegisterGeneric_DeviceClassIsNotAvailable_IsNotAddedToAvailableDeviceClasses()
    {
        // Arrange     
        GcLibrary.Init(false);

        // Act
        GcLibrary.Register<UnavailableCam>();

        // Assert
        Assert.DoesNotContain(UnavailableCam.DeviceClassInfo, GcLibrary.GetAvailableDeviceClasses());
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
    public void RegisterNonGeneric_IsNotInitialized_ThrowsInvalidOperationException()
    {
        // Act/Assert
        Assert.Throws<InvalidOperationException>(() => GcLibrary.Register(typeof(VirtualCam)));
    }

    [TestMethod]
    public void RegisterNonGeneric_DeviceClassIsNotRegistered_DeviceClassIsRegistered()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Register(typeof(VirtualCam));

        // Assert
        Assert.Contains(VirtualCam.DeviceClassInfo, GcLibrary.GetAvailableDeviceClasses());
        Assert.Contains(VirtualCam.DeviceClassInfo, GcLibrary.GetRegisteredDeviceClasses());
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
    public void RegisterNonGeneric_DeviceClassIsNotAvailable_IsNotAddedToAvailableDeviceClasses()
    {
        // Arrange     
        GcLibrary.Init(false);

        // Act
        GcLibrary.Register(typeof(UnavailableCam));

        // Assert
        Assert.DoesNotContain(UnavailableCam.DeviceClassInfo, GcLibrary.GetAvailableDeviceClasses());
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
        Assert.DoesNotContain(VirtualCam.DeviceClassInfo, GcLibrary.GetAvailableDeviceClasses());
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
        Assert.DoesNotContain(VirtualCam.DeviceClassInfo, GcLibrary.GetAvailableDeviceClasses());
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

    #region GetAvailableDeviceClassesTests

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

    #endregion

    #region GetRegisteredDeviceClassesTests

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
        // Act/Assert
        Assert.Throws<InvalidOperationException>(GcLibrary.GetRegisteredDeviceClasses);
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
    public void Close_GetAvailableDeviceClasses_ThrowsInvalidOperationException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Close();

        // Assert
        Assert.Throws<InvalidOperationException>(GcLibrary.GetAvailableDeviceClasses);
    }

    [TestMethod]
    public void Close_GetRegisteredDeviceClasses_ThrowsInvalidOperationException()
    {
        // Arrange
        GcLibrary.Init(false);

        // Act
        GcLibrary.Close();

        // Assert
        Assert.Throws<InvalidOperationException>(GcLibrary.GetRegisteredDeviceClasses);
    }

    #endregion
}