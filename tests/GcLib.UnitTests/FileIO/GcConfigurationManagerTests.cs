using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using GcLib.Utilities.FileIO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

[TestClass]
[DoNotParallelize]
public class GcConfigurationManagerTests
{
    private const string DeviceId = "ConfigTestCam";

    private VirtualCam _device;
    private VirtualCam _otherDevice;
    private GcConfigurationManager _manager;
    private string _directory;
    private string _path;

    [TestInitialize]
    public void TestInitialize()
    {
        _directory = Path.Combine(Path.GetTempPath(), "GcConfigurationManagerTests_" + Guid.NewGuid().ToString("N"));
        _ = Directory.CreateDirectory(_directory);
        _path = Path.Combine(_directory, "config.xml");

        _device = new VirtualCam(DeviceId);
        _manager = new GcConfigurationManager(_device);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _manager?.Dispose();
        _device?.Close();
        _otherDevice?.Close();

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    private void WriteFile(string content) => File.WriteAllText(_path, content);

    private static string ConfigFile(string body, string device = DeviceId) =>
        $"<?xml version=\"1.0\" encoding=\"utf-8\"?><ParameterList Device=\"{device}\">{body}</ParameterList>";

    #region Constructor

    [TestMethod]
    public void Constructor_NullCamera_Throws()
    {
        _ = Assert.Throws<ArgumentNullException>(() => new GcConfigurationManager(null));
    }

    [TestMethod]
    public void Constructor_SetsDeviceName()
    {
        Assert.AreEqual(DeviceId, _manager.DeviceName);
    }

    [TestMethod]
    public void PropertyCount_NothingLoaded_IsZero()
    {
        Assert.AreEqual(0u, _manager.PropertyCount);
    }

    #endregion

    #region Load

    [TestMethod]
    public void Load_ValidFile_ReadsAllParameters()
    {
        // Arrange
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"Abc\" /><Parameter Name=\"TestInteger\" Value=\"7\" />"));

        // Act
        _manager.Load(_path);

        // Assert
        Assert.AreEqual(2u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_EmptyParameterList_DoesNotHang()
    {
        // Arrange
        WriteFile($"<?xml version=\"1.0\"?><ParameterList Device=\"{DeviceId}\" />");

        // Act
        var task = Task.Run(() => _manager.Load(_path), TestContext.CancellationToken);

        // Assert
        Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10), TestContext.CancellationToken), "Load did not return.");
        Assert.AreEqual(0u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_EmptyParameterListWithEndTag_ReadsZeroParameters()
    {
        // Arrange
        WriteFile(ConfigFile(""));

        // Act
        _manager.Load(_path);

        // Assert
        Assert.AreEqual(0u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_CommentsAndWhitespace_AreIgnored()
    {
        // Arrange
        WriteFile(ConfigFile("\n  <!-- comment -->\n  <Parameter Name=\"TestString\" Value=\"Abc\" />\n  <?pi data?>\n  <!-- another -->\n"));

        // Act
        _manager.Load(_path);

        // Assert
        Assert.AreEqual(1u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_EmptyValue_IsAccepted()
    {
        // Arrange
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"\" />"));

        // Act
        _manager.Load(_path);

        // Assert
        Assert.AreEqual(1u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_ParameterWithoutName_Throws()
    {
        WriteFile(ConfigFile("<Parameter Value=\"1\" />"));

        _ = Assert.Throws<InvalidDataException>(() => _manager.Load(_path));
        Assert.AreEqual(0u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_ParameterWithoutValue_Throws()
    {
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" />"));

        _ = Assert.Throws<InvalidDataException>(() => _manager.Load(_path));
    }

    [TestMethod]
    public void Load_MissingRootElement_ThrowsInvalidData()
    {
        WriteFile("<?xml version=\"1.0\"?><Something Device=\"" + DeviceId + "\" />");

        _ = Assert.Throws<InvalidDataException>(() => _manager.Load(_path));
    }

    [TestMethod]
    public void Load_WrongDevice_ThrowsInvalidOperation()
    {
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"Abc\" />", device: "SomeOtherDevice"));

        _ = Assert.Throws<InvalidOperationException>(() => _manager.Load(_path));
    }

    [TestMethod]
    public void Load_MissingDeviceAttribute_ThrowsInvalidOperation()
    {
        WriteFile("<ParameterList><Parameter Name=\"TestString\" Value=\"Abc\" /></ParameterList>");

        _ = Assert.Throws<InvalidOperationException>(() => _manager.Load(_path));
    }

    [TestMethod]
    public void Load_MalformedXml_ThrowsXmlException()
    {
        WriteFile($"<ParameterList Device=\"{DeviceId}\"><Parameter Name=\"TestString\" Value=\"Abc\" />");

        _ = Assert.Throws<XmlException>(() => _manager.Load(_path));
    }

    [TestMethod]
    public void Load_MalformedXml_KeepsPreviousProperties()
    {
        // Arrange
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"Abc\" />"));
        _manager.Load(_path);
        WriteFile($"<ParameterList Device=\"{DeviceId}\"><Parameter Name=\"A\" Value=\"1\" /><Parameter Name=\"B\" Value=\"2\" />");

        // Act
        _ = Assert.Throws<XmlException>(() => _manager.Load(_path));

        // Assert
        Assert.AreEqual(1u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_FileNotFound_Throws()
    {
        _ = Assert.Throws<FileNotFoundException>(() => _manager.Load(Path.Combine(_directory, "missing.xml")));
    }

    [TestMethod]
    public void Load_NullOrEmptyPath_Throws()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _manager.Load(null));
        _ = Assert.Throws<ArgumentException>(() => _manager.Load(""));
    }

    [TestMethod]
    public void Load_ReplacesPreviousProperties()
    {
        // Arrange
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"Abc\" /><Parameter Name=\"TestInteger\" Value=\"7\" />"));
        _manager.Load(_path);
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"Xyz\" />"));

        // Act
        _manager.Load(_path);

        // Assert
        Assert.AreEqual(1u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Load_ReleasesFile()
    {
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"Abc\" />"));
        _manager.Load(_path);

        File.Delete(_path);

        Assert.IsFalse(File.Exists(_path));
    }

    #endregion

    #region Store

    [TestMethod]
    public void Store_NullList_Throws()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _manager.Store(null));
    }

    [TestMethod]
    public void Store_DeviceParameters_SkipsCommandsAndInvisible()
    {
        // Arrange
        var parameters = _device.Parameters.ToList(GcVisibility.Guru);

        // Act
        _manager.Store(parameters);
        _manager.Save(_path);

        // Assert
        var names = XmlNames(_path);
        Assert.IsGreaterThan(0, names.Length);
        Assert.IsTrue(names.Contains("TestString"));
        Assert.IsFalse(names.Contains("TestCommand"));
        foreach (GcParameter parameter in parameters.Where(p => p is GcCommand || p.Visibility == GcVisibility.Invisible))
            Assert.IsFalse(names.Contains(parameter.Name), parameter.Name);
    }

    [TestMethod]
    public void Store_SelectorIsPlacedBeforeSelectedParameter()
    {
        // Act
        _manager.Store(_device.Parameters.ToList(GcVisibility.Guru));
        _manager.Save(_path);

        // Assert
        var names = XmlNames(_path);
        int selector = Array.IndexOf(names, "DeviceTemperatureSelector");
        int selected = Array.IndexOf(names, "DeviceTemperature");
        Assert.IsGreaterThanOrEqualTo(0, selector);
        Assert.IsGreaterThanOrEqualTo(0, selected);
        Assert.IsLessThan(selected, selector);
    }

    [TestMethod]
    public void Store_ReplacesPreviousProperties()
    {
        // Arrange
        WriteFile(ConfigFile("<Parameter Name=\"A\" Value=\"1\" />"));
        _manager.Load(_path);

        // Act
        _manager.Store(_device.Parameters.ToList(GcVisibility.Guru));

        // Assert
        Assert.IsGreaterThan(1u, _manager.PropertyCount);
    }

    #endregion

    #region Save

    [TestMethod]
    public void Save_NothingLoaded_ThrowsInvalidOperation()
    {
        _ = Assert.Throws<InvalidOperationException>(() => _manager.Save(_path));
        Assert.IsFalse(File.Exists(_path));
    }

    [TestMethod]
    public void Save_NullOrEmptyPath_Throws()
    {
        _manager.Store([]);

        _ = Assert.Throws<ArgumentNullException>(() => _manager.Save(null));
        _ = Assert.Throws<ArgumentException>(() => _manager.Save(""));
    }

    [TestMethod]
    public void Save_EmptyList_RoundTripsThroughLoad()
    {
        // Arrange
        _manager.Store([]);
        _manager.Save(_path);

        using var other = new GcConfigurationManager(_device);

        // Act
        var task = Task.Run(() => other.Load(_path), TestContext.CancellationToken);

        // Assert
        Assert.IsTrue(task.Wait(TimeSpan.FromSeconds(10), TestContext.CancellationToken), "Load did not return.");
        Assert.AreEqual(0u, other.PropertyCount);
    }

    [TestMethod]
    public void Save_WritesDeviceNameAndParameters()
    {
        // Arrange
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"a&amp;b &lt;c&gt;\" />"));
        _manager.Load(_path);
        string other = Path.Combine(_directory, "copy.xml");

        // Act
        _manager.Save(other);

        // Assert
        var doc = new XmlDocument();
        doc.Load(other);
        Assert.AreEqual(DeviceId, doc.DocumentElement.GetAttribute("Device"));
        Assert.AreEqual("a&b <c>", doc.DocumentElement.FirstChild.Attributes["Value"].Value);
    }

    [TestMethod]
    public void Save_ExistingFile_IsOverwritten()
    {
        // Arrange
        WriteFile("old content");
        _manager.Store([]);

        // Act
        _manager.Save(_path);

        // Assert
        Assert.Contains("ParameterList", File.ReadAllText(_path));
    }

    [TestMethod]
    public void Save_LeavesNoTemporaryFiles()
    {
        _manager.Store([]);

        _manager.Save(_path);

        Assert.AreSequenceEqual([_path], Directory.GetFiles(_directory));
    }

    [TestMethod]
    public void Save_FailureKeepsExistingFileAndLeavesNoTemporaryFiles()
    {
        // Arrange
        WriteFile("original");
        WriteFile(ConfigFile("<Parameter Name=\"TestString\" Value=\"Abc\" />"));
        _manager.Load(_path);
        string original = File.ReadAllText(_path);

        // The target is locked, so the final move fails.
        using var locked = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.Read);

        // Act
        var exception = Assert.Throws<Exception>(() => _manager.Save(_path));

        // Assert
        Assert.IsTrue(exception is IOException or UnauthorizedAccessException, exception.GetType().Name);
        locked.Dispose();
        Assert.AreEqual(original, File.ReadAllText(_path));
        Assert.AreSequenceEqual([_path], Directory.GetFiles(_directory));
    }

    [TestMethod]
    public void Save_DirectoryDoesNotExist_Throws()
    {
        _manager.Store([]);

        _ = Assert.Throws<DirectoryNotFoundException>(() => _manager.Save(Path.Combine(_directory, "missing", "config.xml")));
    }

    #endregion

    #region Restore

    [TestMethod]
    public void Restore_NothingLoaded_ThrowsInvalidOperation()
    {
        _ = Assert.Throws<InvalidOperationException>(() => _manager.Restore(_device));
    }

    [TestMethod]
    public void Restore_NullCamera_Throws()
    {
        _manager.Store([]);

        _ = Assert.Throws<ArgumentNullException>(() => _manager.Restore(null));
    }

    [TestMethod]
    public void Restore_OtherDevice_ThrowsInvalidOperation()
    {
        // Arrange
        _otherDevice = new VirtualCam("AnotherCam");
        _manager.Store([]);

        // Act & Assert
        _ = Assert.Throws<InvalidOperationException>(() => _manager.Restore(_otherDevice));
    }

    [TestMethod]
    public void Restore_AppliesStoredValues()
    {
        // Arrange
        _device.Parameters.SetParameterValue("TestString", "Stored");
        _device.Parameters.SetParameterValue("TestInteger", "77");
        _device.Parameters.SetParameterValue("TestFloat", "4.5");
        _device.Parameters.SetParameterValue("TestBoolean", "True");
        _device.Parameters.SetParameterValue("TestEnumeration", "Monday");
        _manager.Store(_device.Parameters.ToList(GcVisibility.Guru));
        _manager.Save(_path);

        _device.Parameters.SetParameterValue("TestString", "Changed");
        _device.Parameters.SetParameterValue("TestInteger", "1");
        _device.Parameters.SetParameterValue("TestFloat", "1.5");
        _device.Parameters.SetParameterValue("TestBoolean", "False");
        _device.Parameters.SetParameterValue("TestEnumeration", "Tuesday");

        using var loader = new GcConfigurationManager(_device);
        loader.Load(_path);

        // Act
        var failed = loader.Restore(_device);

        // Assert
        Assert.AreEqual("Stored", _device.Parameters.GetParameterValue("TestString"));
        Assert.AreEqual("77", _device.Parameters.GetParameterValue("TestInteger"));
        Assert.AreEqual("4.5", _device.Parameters.GetParameterValue("TestFloat"));
        Assert.AreEqual("True", _device.Parameters.GetParameterValue("TestBoolean"), ignoreCase: true);
        Assert.AreEqual("Monday", _device.Parameters.GetParameterValue("TestEnumeration"));
        Assert.IsEmpty(failed);
    }

    [TestMethod]
    public void Restore_UnknownParameter_IsIgnored()
    {
        // Arrange
        WriteFile(ConfigFile("<Parameter Name=\"NoSuchParameter\" Value=\"1\" /><Parameter Name=\"TestString\" Value=\"Abc\" />"));
        _manager.Load(_path);

        // Act
        var failed = _manager.Restore(_device);

        // Assert
        Assert.IsEmpty(failed);
        Assert.AreEqual("Abc", _device.Parameters.GetParameterValue("TestString"));
    }

    [TestMethod]
    public void Restore_ReadOnlyParameter_IsSkippedWithoutFailure()
    {
        // Arrange
        string before = _device.Parameters.GetParameterValue("DeviceTemperature");
        WriteFile(ConfigFile("<Parameter Name=\"DeviceTemperature\" Value=\"-123\" />"));
        _manager.Load(_path);

        // Act
        var failed = _manager.Restore(_device);

        // Assert
        Assert.IsEmpty(failed);
        Assert.AreEqual(before, _device.Parameters.GetParameterValue("DeviceTemperature"));
    }

    [TestMethod]
    public void Restore_InvalidValue_IsReportedAndRestoreContinues()
    {
        // Arrange
        WriteFile(ConfigFile(
            "<Parameter Name=\"TestInteger\" Value=\"not a number\" />" +
            "<Parameter Name=\"TestEnumeration\" Value=\"NotADay\" />" +
            "<Parameter Name=\"TestString\" Value=\"Abc\" />"));
        _manager.Load(_path);

        // Act
        var failed = _manager.Restore(_device);

        // Assert
        Assert.AreSequenceEqual(["TestInteger", "TestEnumeration"], [.. failed]);
        Assert.AreEqual("Abc", _device.Parameters.GetParameterValue("TestString"));
    }

    [TestMethod]
    public void Restore_ValuesAlreadyMatch_ReturnsEmpty()
    {
        // Arrange
        _manager.Store(_device.Parameters.ToList(GcVisibility.Guru));

        // Act
        var failed = _manager.Restore(_device);

        // Assert
        Assert.IsEmpty(failed);
    }

    #endregion

    #region Dispose

    [TestMethod]
    public void Dispose_ClearsProperties()
    {
        _manager.Store(_device.Parameters.ToList(GcVisibility.Guru));

        _manager.Dispose();

        Assert.AreEqual(0u, _manager.PropertyCount);
    }

    [TestMethod]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _manager.Dispose();
        _manager.Dispose();
    }

    [TestMethod]
    public void AfterDispose_OperationsThrowObjectDisposed()
    {
        _manager.Dispose();

        _ = Assert.Throws<ObjectDisposedException>(() => _manager.Load(_path));
        _ = Assert.Throws<ObjectDisposedException>(() => _manager.Save(_path));
        _ = Assert.Throws<ObjectDisposedException>(() => _manager.Store([]));
        _ = Assert.Throws<ObjectDisposedException>(() => _manager.Restore(_device));
    }

    #endregion

    #region Helpers

    private static string[] XmlNames(string path)
    {
        var doc = new XmlDocument();
        doc.Load(path);
        return [.. doc.DocumentElement.ChildNodes.Cast<XmlNode>().Select(n => n.Attributes["Name"].Value)];
    }

    public TestContext TestContext { get; set; }

    #endregion
}
