using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests.Devices;

[TestClass]
public class GcDeviceClassInfoTests
{
    /// <summary>
    /// Mocked device for unit testing.
    /// </summary>
    private readonly Mock<GcDevice> _device = new();

    [TestMethod]
    public void Constructor_WithSubclass_SetsProperties()
    {
        // Arrange/Act
        var info = new GcDeviceClassInfo("MyApi", "1.2.3", _device.Object.GetType());

        // Assert
        Assert.AreEqual("MyApi", info.Name);
        Assert.AreEqual("1.2.3", info.Version);
        Assert.IsInstanceOfType<GcDevice>(_device.Object);
        Assert.AreEqual(_device.Object.GetType(), info.DeviceType);
    }

    [TestMethod]
    public void Constructor_WithNonSubclass_ThrowsArgumentException()
    {
        // Arrange/Act/Assert
        Assert.Throws<ArgumentException>(() => { var _ = new GcDeviceClassInfo("Bad", "0.0", typeof(object)); });
    }

    [TestMethod]
    public void ToString_ReturnsDeviceTypeToString()
    {
        // Arrange
        var info = new GcDeviceClassInfo("X", "Y", _device.Object.GetType());

        // Act/Assert
        Assert.AreEqual(_device.Object.GetType().ToString(), info.ToString());
    }
}