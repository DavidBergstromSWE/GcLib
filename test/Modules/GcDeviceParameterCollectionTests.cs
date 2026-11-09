using System;
using System.Collections.Generic;
using System.Reflection;
using GcLib.UnitTests.Utilities;
using GcLib.Utilities.Collections;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace GcLib.UnitTests.Modules;

[TestClass]
public class GcDeviceParameterCollectionTests
{
    private VirtualCam _device;
    private IReadOnlyParameterCollection _parameters;

    [TestInitialize]
    public void TestInitialize()
    {
        _device = new VirtualCam("VirtualCamTestDevice");
        _parameters = _device.Parameters;
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _device?.Close();
    }

    [TestMethod]
    public void Name_ReturnsDeviceUniqueId()
    {
        Assert.AreEqual(_device.DeviceInfo.UniqueID, _parameters.Name);
    }

    [TestMethod]
    public void IsImplemented_ExistingParameter_ReturnsTrue()
    {
        Assert.IsTrue(_parameters.IsImplemented("DeviceUserID"));
    }

    [TestMethod]
    public void IsImplemented_MissingParameter_ReturnsFalse()
    {
        Assert.IsFalse(_parameters.IsImplemented("NotARealParameter"));
    }

    [TestMethod]
    public void StringIndexer_ExistingParameter_ReturnsParameter()
    {
        var parameter = _parameters["DeviceUserID"];

        Assert.IsNotNull(parameter);
        Assert.AreEqual("DeviceUserID", parameter.Name);
    }

    [TestMethod]
    public void StringIndexer_MissingParameter_ThrowsKeyNotFoundException()
    {
        Assert.Throws<KeyNotFoundException>(() => _ = _parameters["NotARealParameter"]);
    }

    [TestMethod]
    public void GetParameterValue_CommandParameter_ReturnsNull()
    {
        var value = _parameters.GetParameterValue("TimestampReset");

        Assert.IsNull(value);
    }

    [TestMethod]
    public void SetParameterValue_WritableParameter_UpdatesValueAndRaisesInvalidate()
    {
        int invalidateCount = 0;
        string invalidatedParameter = null;
        _device.ParameterInvalidate += (_, e) =>
        {
            invalidateCount++;
            invalidatedParameter = e.ParameterName;
        };

        _parameters.SetParameterValue("DeviceUserID", "UnitTestName");

        Assert.AreEqual("UnitTestName", _parameters.GetParameterValue("DeviceUserID"));
        Assert.AreEqual(1, invalidateCount);
        Assert.AreEqual("DeviceUserID", invalidatedParameter);
    }

    [TestMethod]
    public void ExecuteParameterCommand_CommandParameter_RaisesInvalidate()
    {
        int invalidateCount = 0;
        string invalidatedParameter = null;
        _device.ParameterInvalidate += (_, e) =>
        {
            invalidateCount++;
            invalidatedParameter = e.ParameterName;
        };

        _parameters.ExecuteParameterCommand("TimestampReset");

        Assert.AreEqual(1, invalidateCount);
        Assert.AreEqual("TimestampReset", invalidatedParameter);
    }

    [TestMethod]
    public void GetParameter_ExistingTypedParameters_ReturnExpectedTypes()
    {
        Assert.IsInstanceOfType<GcString>(_parameters.GetParameter("DeviceUserID"));
        Assert.IsInstanceOfType<GcCommand>(_parameters.GetParameter("TimestampReset"));
        Assert.IsNull(_parameters.GetParameter("NotARealParameter"));
    }

    [TestMethod]
    public void GetPropertyList_ExcludesCommands()
    {
        var propertyList = _parameters.GetPropertyList(GcVisibility.Guru);

        Assert.DoesNotContain(p => p.Key == "TimestampReset", propertyList);
    }

    [TestMethod]
    public void Constructor_NullDevice_ThrowsArgumentNullException()
    {
        AssertConstructorArgumentNull(() => CreateCollection(device: null, [], []), "device");
    }

    [TestMethod]
    public void Constructor_NullParameters_ThrowsArgumentNullException()
    {
        AssertConstructorArgumentNull(() => CreateCollection(_device, null, []), "parameters");
    }

    [TestMethod]
    public void Constructor_NullFailedParameters_ThrowsArgumentNullException()
    {
        AssertConstructorArgumentNull(() => CreateCollection(_device, [], null), "failedParameters");
    }

    [TestMethod]
    public void Constructor_NullDeviceInfo_ThrowsArgumentNullException()
    {
        var mockDevice = new Mock<MockableCam>();
        AssertConstructorArgumentNull(() => CreateCollection(mockDevice.Object, [], []), "device.DeviceInfo");
    }

    private static object CreateCollection(GcDevice device, List<GcParameter> parameters, List<string> failedParameters)
    {
        var collectionType = typeof(GcDevice).GetNestedType("GcDeviceParameterCollection", BindingFlags.NonPublic);
        return Activator.CreateInstance(collectionType, device, parameters, failedParameters);
    }

    private static void AssertConstructorArgumentNull(Action action, string expectedParamName)
    {
        var ex = Assert.Throws<TargetInvocationException>(action);
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<ArgumentNullException>(ex.InnerException);
        Assert.AreEqual(expectedParamName, ((ArgumentNullException)ex.InnerException).ParamName);
    }
}
