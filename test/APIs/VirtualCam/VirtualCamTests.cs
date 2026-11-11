using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.APIs;

[TestClass]
public class VirtualCamTests
{
    private VirtualCam _device;

    [TestInitialize]
    public void TestInitialize()
    {

    }

    [TestCleanup]
    public void TestCleanUp()
    {
        _device?.Close();
    }

    [TestMethod]
    public void EnumerateDevices_CountIsTwo()
    {
        // Act
        var devices = VirtualCam.EnumerateDevices();

        // Assert
        Assert.HasCount(2, devices);
    }

    [TestMethod]
    public void EnumerateDevices_NumberOfAvailableDevicesIsUpdated_CountMatches()
    {
        // Arrange
        var original = VirtualCam.NumberOfAvailableDevices;
        VirtualCam.NumberOfAvailableDevices = 3;

        try
        {
            // Act
            var devices = VirtualCam.EnumerateDevices();

            // Assert
            Assert.HasCount(3, devices);
            Assert.AreEqual("VirtualCam3", devices[2].UniqueID);
        }
        finally
        {
            VirtualCam.NumberOfAvailableDevices = original;
        }
    }

    [TestMethod]
    public void CreateDevice_UsesDeviceInfoUniqueId()
    {
        // Arrange
        var info = new GcDeviceInfo("Vendor", "Model", "SN", "MyUniqueId", VirtualCam.DeviceClassInfo, userDefinedName: "UserName");

        // Act
        var device = VirtualCam.CreateDevice(info);

        // Assert
        Assert.AreEqual("MyUniqueId", device.DeviceInfo.UniqueID);
        Assert.AreEqual("MyUniqueId", device.DeviceInfo.UserDefinedName);

        // Cleanup
        device.Close();
    }

    [TestMethod]
    public void VirtualCam_ValidateInstantiation()
    {
        // Arrange
        var ID = "TestCam";

        // Act
        _device = new VirtualCam(ID);

        // Assert
        Assert.IsGreaterThan<uint>(0, _device.PayloadSize);
        Assert.IsNotEmpty(_device.Parameters);
        Assert.IsGreaterThan<uint>(0, _device.BufferCapacity);
        Assert.IsFalse(_device.IsAcquiring);
        Assert.IsNotNull(_device.DeviceInfo);
        Assert.IsTrue(_device.DeviceInfo.IsOpen);
        Assert.IsFalse(_device.DeviceInfo.IsAccessible);
        Assert.AreEqual(nameof(VirtualCam), _device.DeviceInfo.ModelName);
        Assert.IsNotNull(_device.DeviceInfo.SerialNumber);
        Assert.AreEqual(ID, _device.DeviceInfo.UniqueID);
        Assert.AreEqual(ID, _device.DeviceInfo.UserDefinedName);
        Assert.AreEqual(typeof(VirtualCam), _device.DeviceInfo.DeviceClassInfo.DeviceType);
        Assert.AreEqual<uint>(0, _device.GetNumDataStreams());
    }

    [TestMethod]
    public void StartAcquisition_IsAcquiringisTrue()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        _device.StartAcquisition();

        // Assert
        Assert.IsTrue(_device.IsAcquiring);
    }

    [TestMethod]
    public void StartAcquisition_AcquisitionStartedEventIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        int eventCounter = 0;
        void eventHandler(object s, EventArgs e) { eventCounter++; }
        _device.AcquisitionStarted += eventHandler;

        // Act
        _device.StartAcquisition();

        // Assert
        Assert.AreEqual(1, eventCounter);

        // Cleanup
        _device.AcquisitionStarted -= eventHandler;
    }

    [TestMethod]
    public void StartAcquisition_AlreadyAcquiring_ThrowsInvalidOperationException()
    {
        // Arrange
        _device = new VirtualCam();
        _device.StartAcquisition();

        try
        {
            // Act/Assert
            Assert.Throws<InvalidOperationException>(() => _device.StartAcquisition());
        }
        finally
        {
            _device.StopAcquisition();
        }
    }

    [TestMethod]
    public async Task StartAcquisition_NewBufferEventIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("Width", "80");
        _device.Parameters.SetParameterValue("Height", "60");
        _device.Parameters.SetParameterValue("AcquisitionFrameRate", "60");
        int eventCounter = 0;
        var firstBufferReceived = new TaskCompletionSource<NewBufferEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously); // Use TaskCompletionSource to signal when the first buffer is received
        void eventHandler(object s, NewBufferEventArgs e)
        {
            // Increment the event counter and set the result of the TaskCompletionSource when the first buffer is received
            Interlocked.Increment(ref eventCounter);
            firstBufferReceived.TrySetResult(e);
        }
        _device.NewBuffer += eventHandler;

        try
        {
            // Act
            _device.StartAcquisition();
            var completedTask = await Task.WhenAny(firstBufferReceived.Task, Task.Delay(2000, CancellationToken.None)); // Wait for either the first buffer to be received or a timeout of 2 seconds

            // Assert
            Assert.AreSame(firstBufferReceived.Task, completedTask, "Timed out waiting for NewBuffer event.");
            Assert.IsGreaterThan(0, eventCounter);
            Assert.IsNotNull(firstBufferReceived.Task.Result);
            Assert.IsNotEmpty(firstBufferReceived.Task.Result.Buffer.ImageData);
        }
        finally
        {
            // Cleanup
            _device.StopAcquisition();
            _device.NewBuffer -= eventHandler;
        }
    }

    [TestMethod]
    public void StopAcquisition_IsAcquring_IsNotAcquiring()
    {
        // Arrange
        _device = new VirtualCam();
        _device.StartAcquisition();

        // Act
        _device.StopAcquisition();

        // Assert
        Assert.IsFalse(_device.IsAcquiring);
    }

    [TestMethod]
    public void StopAcquisition_AcquisitionStoppedIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        int eventCounter = 0;
        void eventHandler(object s, EventArgs e) { eventCounter++; }
        _device.AcquisitionStopped += eventHandler;
        _device.StartAcquisition();

        // Act
        _device.StopAcquisition();

        // Assert
        Assert.AreEqual(1, eventCounter);

        // Cleanup
        _device.AcquisitionStopped -= eventHandler;
    }

    [TestMethod]
    public void StopAcquisition_IsNotRunning_AcquisitionStoppedIsNotRaised()
    {
        // Arrange
        _device = new VirtualCam();
        int eventCounter = 0;
        void eventHandler(object s, EventArgs e) { eventCounter++; }
        _device.AcquisitionStopped += eventHandler;

        // Act
        _device.StopAcquisition();

        // Assert
        Assert.AreEqual(0, eventCounter);

        // Cleanup
        _device.AcquisitionStopped -= eventHandler;
    }

    [TestMethod]
    public void FetchImage_BufferIsNotNull()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        bool success = _device.FetchImage(out GcBuffer buffer);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(buffer);
    }

    [TestMethod]
    public void FetchImage_TimedOut_BufferIsNull()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        bool success = _device.FetchImage(buffer: out GcBuffer buffer, timeout: 0);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(buffer);
    }

    [TestMethod]
    [DataRow("TestString")]
    [DataRow("TestInteger")]
    [DataRow("TestFloat")]
    [DataRow("TestBoolean")]
    [DataRow("TestEnumeration")]
    public void GetParameter_ParameterExists_IsOfCorrectType(string parameterName)
    {
        // Arrange
        _device = new VirtualCam();
        var expectedTypeName = parameterName[4..];

        // Act
        var parameter = _device.Parameters.GetParameter(parameterName);
        var actualTypeName = parameter.Type.ToString();

        // Assert
        Assert.IsNotNull(parameter);
        Assert.AreEqual(expectedTypeName, actualTypeName);
    }

    [TestMethod]
    public void GetParameter_ParameterDoesNotExist_IsNull()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        var parameter = _device.Parameters.GetParameter("FakeParameter");

        // Assert
        Assert.IsNull(parameter);
    }

    [TestMethod]
    [DataRow("TestString")]
    [DataRow("TestInteger")]
    [DataRow("TestFloat")]
    [DataRow("TestBoolean")]
    [DataRow("TestEnumeration")]
    public void GetParameterValue_ParameterExists_IsNotNull(string parameterName)
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        var actualValue = _device.Parameters.GetParameterValue(parameterName);

        // Assert
        Assert.IsNotNull(actualValue);
    }

    [TestMethod]
    public void GetParameterValue_ParameterDoesNotExist_IsNull()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        var actualValue = _device.Parameters.GetParameterValue("FakeParameter");

        // Assert
        Assert.IsNull(actualValue);
    }

    [TestMethod]
    [DataRow("TestString", "GoodbyeUniverse")]
    [DataRow("TestInteger", "66")]
    [DataRow("TestFloat", "2.7")]
    [DataRow("TestBoolean", "True")]
    [DataRow("TestEnumeration", "Monday")]
    public void SetParameterValue_ValidValue_ValueIsChanged(string parameterName, string expectedValue)
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        _device.Parameters.SetParameterValue(parameterName, expectedValue);
        var actualValue = _device.Parameters.GetParameterValue(parameterName);

        // Assert
        Assert.AreEqual(expectedValue, actualValue);
    }

    [TestMethod]
    public void SetParameterValue_ParameterDoesNotExist_DoesNotThrow()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        _device.Parameters.SetParameterValue("FakeParameter", "FakeValue");
    }

    [TestMethod]
    public void ExecuteParameterCommand_ParameterExist_DoesNotThrow()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        _device.Parameters.ExecuteParameterCommand("TestCommand");
    }

    [TestMethod]
    public void ExecuteParameterCommand_ParameterDoesNotExist_DoesNotThrow()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        _device.Parameters.ExecuteParameterCommand("FakeCommand");
    }

    [TestMethod]
    public void ProtectedGetter_GetBoolean_MissingParameter_ThrowsMissingMemberException()
    {
        // Arrange
        _device = new VirtualCam();

        // Act/Assert
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeNonPublicMethod(_device, "GetBoolean", "FakeParameter"));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<MissingMemberException>(ex.InnerException);
    }

    [TestMethod]
    public void ProtectedGetter_GetString_MissingParameter_ThrowsMissingMemberException()
    {
        // Arrange
        _device = new VirtualCam();

        // Act/Assert
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeNonPublicMethod(_device, "GetString", "FakeParameter"));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<MissingMemberException>(ex.InnerException);
    }

    [TestMethod]
    public void ProtectedCommand_ExecuteParameterCommand_MissingParameter_ThrowsMissingMemberException()
    {
        // Arrange
        _device = new VirtualCam();

        // Act/Assert
        var ex = Assert.Throws<TargetInvocationException>(() => InvokeNonPublicMethod(_device, "ExecuteParameterCommand", "FakeCommand"));
        Assert.IsNotNull(ex.InnerException);
        Assert.IsInstanceOfType<MissingMemberException>(ex.InnerException);
    }

    [TestMethod]
    public void SetParameterValue_ParameterInvalidateIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        int eventCounter = 0;
        void eventHandler(object s, EventArgs e) { eventCounter++; }
        _device.ParameterInvalidate += eventHandler;

        // Act
        _device.Parameters.SetParameterValue("TestString", "GoodbyeUniverse");

        // Assert
        Assert.AreEqual(1, eventCounter);

        // Cleanup
        _device.ParameterInvalidate -= eventHandler;
    }

    [TestMethod]
    public void ExecuteParameterCommand_ParameterInvalidateIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        int eventCounter = 0;
        void eventHandler(object s, EventArgs e) { eventCounter++; }
        _device.ParameterInvalidate += eventHandler;

        // Act
        _device.Parameters.ExecuteParameterCommand("TestCommand");

        // Assert
        Assert.AreEqual(1, eventCounter);

        // Cleanup
        _device.ParameterInvalidate -= eventHandler;
    }

    [TestMethod]
    public async Task ExecuteParameterCommand_ForceDisconnect_ConnectionLostIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void eventHandler(object s, EventArgs e) => disconnected.TrySetResult(true);
        _device.ConnectionLost += eventHandler;

        try
        {
            // Act
            _device.Parameters.ExecuteParameterCommand("ForceDisconnect");
            var completedTask = await Task.WhenAny(disconnected.Task, Task.Delay(2000, CancellationToken.None));

            // Assert
            Assert.AreSame(disconnected.Task, completedTask, "Timed out waiting for ConnectionLost event.");
            Assert.IsTrue(disconnected.Task.Result);
        }
        finally
        {
            _device.ConnectionLost -= eventHandler;
        }
    }

    [TestMethod]
    public async Task StartAcquisition_AcquisitionFailureConnectionLost_ConnectionLostIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("AcquisitionFailure", "True");
        _device.Parameters.SetParameterValue("AcquisitionFailureEvent", "ConnectionLost");
        _device.Parameters.SetParameterValue("AcquisitionTimeToFailure", "10");

        var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void eventHandler(object s, EventArgs e) => disconnected.TrySetResult(true);
        _device.ConnectionLost += eventHandler;

        try
        {
            // Act
            _device.StartAcquisition();
            var completedTask = await Task.WhenAny(disconnected.Task, Task.Delay(2000, CancellationToken.None));

            // Assert
            Assert.AreSame(disconnected.Task, completedTask, "Timed out waiting for ConnectionLost event.");
            Assert.IsTrue(disconnected.Task.Result);
        }
        finally
        {
            _device.StopAcquisition();
            _device.ConnectionLost -= eventHandler;
        }
    }

    [TestMethod]
    public async Task StartAcquisition_AcquisitionFailureAborted_AcquisitionAbortedIsRaised()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("AcquisitionFailure", "True");
        _device.Parameters.SetParameterValue("AcquisitionFailureEvent", "AcquisitionAborted");
        _device.Parameters.SetParameterValue("AcquisitionTimeToFailure", "10");

        var aborted = new TaskCompletionSource<AcquisitionAbortedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        void eventHandler(object s, AcquisitionAbortedEventArgs e) => aborted.TrySetResult(e);
        _device.AcquisitionAborted += eventHandler;

        try
        {
            // Act
            _device.StartAcquisition();
            var completedTask = await Task.WhenAny(aborted.Task, Task.Delay(2000, CancellationToken.None));

            // Assert
            Assert.AreSame(aborted.Task, completedTask, "Timed out waiting for AcquisitionAborted event.");
            Assert.IsNotNull(aborted.Task.Result);
            Assert.IsNotEmpty(aborted.Task.Result.ErrorMessage);
        }
        finally
        {
            _device.StopAcquisition();
            _device.AcquisitionAborted -= eventHandler;
        }
    }

    [TestMethod]
    public async Task StartAcquisition_StopBeforeFailureDelay_FailureEventIsNotRaised()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("AcquisitionFailure", "True");
        _device.Parameters.SetParameterValue("AcquisitionFailureEvent", "ConnectionLost");
        _device.Parameters.SetParameterValue("AcquisitionTimeToFailure", "250");

        var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void eventHandler(object s, EventArgs e) => disconnected.TrySetResult(true);
        _device.ConnectionLost += eventHandler;

        try
        {
            // Act
            _device.StartAcquisition();
            _device.StopAcquisition();
            await Task.Delay(400, CancellationToken.None);

            // Assert
            Assert.IsFalse(disconnected.Task.IsCompleted, "Failure event should not be emitted after acquisition has stopped.");
        }
        finally
        {
            _device.ConnectionLost -= eventHandler;
            _device.StopAcquisition();
        }
    }

    [TestMethod]
    public async Task StartAcquisition_Binning2x2_BufferDimensionsAreReduced()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        _device.Parameters.SetParameterValue("Width", "320");
        _device.Parameters.SetParameterValue("Height", "240");
        _device.Parameters.SetParameterValue("BinningHorizontal", "2");
        _device.Parameters.SetParameterValue("BinningVertical", "2");

        // Act
        var buffer = await AcquireSingleBufferAsync(_device);

        // Assert
        Assert.AreEqual<uint>(160, buffer.Width);
        Assert.AreEqual<uint>(120, buffer.Height);
    }

    [TestMethod]
    public async Task StartAcquisition_ReverseX_MirrorsHorizontalRamp()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        _device.Parameters.SetParameterValue("Width", "80");
        _device.Parameters.SetParameterValue("Height", "60");
        _device.Parameters.SetParameterValue("TestPattern", TestPattern.GrayHorizontalRamp.ToString());
        _device.Parameters.SetParameterValue("ReverseX", "False");
        _device.Parameters.SetParameterValue("ReverseY", "False");

        var baseline = await AcquireSingleBufferAsync(_device);

        _device.Parameters.SetParameterValue("ReverseX", "True");
        var flipped = await AcquireSingleBufferAsync(_device);

        // Assert
        int width = (int)baseline.Width;
        Assert.AreEqual(baseline.ImageData[width - 1], flipped.ImageData[0]);
        Assert.AreEqual(baseline.ImageData[0], flipped.ImageData[width - 1]);
    }

    [TestMethod]
    public async Task StartAcquisition_ReverseY_MirrorsVerticalRamp()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        _device.Parameters.SetParameterValue("Width", "80");
        _device.Parameters.SetParameterValue("Height", "60");
        _device.Parameters.SetParameterValue("TestPattern", TestPattern.GrayVerticalRamp.ToString());
        _device.Parameters.SetParameterValue("ReverseX", "False");
        _device.Parameters.SetParameterValue("ReverseY", "False");

        var baseline = await AcquireSingleBufferAsync(_device);

        _device.Parameters.SetParameterValue("ReverseY", "True");
        var flipped = await AcquireSingleBufferAsync(_device);

        // Assert
        int width = (int)baseline.Width;
        int height = (int)baseline.Height;
        int lastRowStart = (height - 1) * width;

        Assert.AreEqual(baseline.ImageData[lastRowStart], flipped.ImageData[0]);
        Assert.AreEqual(baseline.ImageData[0], flipped.ImageData[lastRowStart]);
    }

    [TestMethod]
    public async Task StartAcquisition_OffsetX_ChangesHorizontalRoiContent()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        _device.Parameters.SetParameterValue("Width", "80");
        _device.Parameters.SetParameterValue("Height", "60");
        _device.Parameters.SetParameterValue("OffsetY", "0");
        _device.Parameters.SetParameterValue("TestPattern", TestPattern.GrayHorizontalRamp.ToString());

        _device.Parameters.SetParameterValue("OffsetX", "0");
        var baseline = await AcquireSingleBufferAsync(_device);

        // Act
        _device.Parameters.SetParameterValue("OffsetX", "80");
        var shifted = await AcquireSingleBufferAsync(_device);

        // Assert
        Assert.AreEqual(baseline.Width, shifted.Width);
        Assert.AreEqual(baseline.Height, shifted.Height);
        Assert.IsTrue(shifted.ImageData[0] > baseline.ImageData[0]);
    }

    [TestMethod]
    public async Task StartAcquisition_OffsetY_ChangesVerticalRoiContent()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        _device.Parameters.SetParameterValue("Width", "80");
        _device.Parameters.SetParameterValue("Height", "60");
        _device.Parameters.SetParameterValue("OffsetX", "0");
        _device.Parameters.SetParameterValue("TestPattern", TestPattern.GrayVerticalRamp.ToString());

        _device.Parameters.SetParameterValue("OffsetY", "0");
        var baseline = await AcquireSingleBufferAsync(_device);

        // Act
        _device.Parameters.SetParameterValue("OffsetY", "60");
        var shifted = await AcquireSingleBufferAsync(_device);

        // Assert
        Assert.AreEqual(baseline.Width, shifted.Width);
        Assert.AreEqual(baseline.Height, shifted.Height);
        Assert.IsTrue(shifted.ImageData[0] > baseline.ImageData[0]);
    }

    [TestMethod]
    public async Task StartAcquisition_ImageDirectory_OffsetIgnored_BinningApplied()
    {
        // Arrange
        _device = new VirtualCam();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var imageData = TestPatternGenerator.CreateImage(320, 240, PixelFormat.Mono8, TestPattern.GrayHorizontalRamp);
        var imagePath = Path.Combine(tempDir, "frame.bmp");
        using (var sourceMat = new Emgu.CV.Mat(240,
                                                320,
                                                EmguHelper.GetDepthType(PixelFormat.Mono8),
                                                (int)GenICamHelper.GetNumChannels(PixelFormat.Mono8)))
        {
            sourceMat.SetTo(imageData);
            sourceMat.Save(imagePath);
        }

        _device.Parameters.SetParameterValue("TestPattern", TestPattern.ImageDirectory.ToString());
        _device.Parameters.SetParameterValue("ImageDirectory", tempDir);
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        _device.Parameters.SetParameterValue("Width", "80");
        _device.Parameters.SetParameterValue("Height", "60");
        _device.Parameters.SetParameterValue("OffsetX", "80");
        _device.Parameters.SetParameterValue("OffsetY", "60");
        _device.Parameters.SetParameterValue("BinningHorizontal", "2");
        _device.Parameters.SetParameterValue("BinningVertical", "2");

        try
        {
            // Act
            var buffer = await AcquireSingleBufferAsync(_device);

            // Assert
            Assert.AreEqual<uint>(160, buffer.Width);
            Assert.AreEqual<uint>(120, buffer.Height);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void SetBinning_ValidatePayloadSize()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        _device.Parameters.SetParameterValue("Width", "320");
        _device.Parameters.SetParameterValue("Height", "240");

        // Act
        _device.Parameters.SetParameterValue("BinningHorizontal", "2");
        _device.Parameters.SetParameterValue("BinningVertical", "2");

        // Assert
        Assert.AreEqual<uint>(19200u, _device.PayloadSize);
    }

    [TestMethod]
    public void SetBinning_UpdatesGeometryAndKeepsSensorDimensions()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("Width", "320");
        _device.Parameters.SetParameterValue("Height", "240");
        _device.Parameters.SetParameterValue("OffsetX", "160");
        _device.Parameters.SetParameterValue("OffsetY", "120");

        // Act
        _device.Parameters.SetParameterValue("BinningHorizontal", "2");
        _device.Parameters.SetParameterValue("BinningVertical", "2");

        // Assert
        Assert.AreEqual("640", _device.Parameters.GetParameterValue("SensorWidth"));
        Assert.AreEqual("480", _device.Parameters.GetParameterValue("SensorHeight"));
        Assert.AreEqual("320", _device.Parameters.GetParameterValue("WidthMax"));
        Assert.AreEqual("240", _device.Parameters.GetParameterValue("HeightMax"));
        Assert.AreEqual("160", _device.Parameters.GetParameterValue("Width"));
        Assert.AreEqual("120", _device.Parameters.GetParameterValue("Height"));
        Assert.AreEqual("80", _device.Parameters.GetParameterValue("OffsetX"));
        Assert.AreEqual("60", _device.Parameters.GetParameterValue("OffsetY"));
    }

    [TestMethod]
    public void SetBinningAndPixelFormat_ValidatePayloadSizeTracksBitDepth()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("Width", "320");
        _device.Parameters.SetParameterValue("Height", "240");
        _device.Parameters.SetParameterValue("BinningHorizontal", "2");
        _device.Parameters.SetParameterValue("BinningVertical", "2");

        // Act
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono8.ToString());
        var payloadMono8 = _device.PayloadSize;

        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.Mono16.ToString());
        var payloadMono16 = _device.PayloadSize;

        // Assert
        Assert.AreEqual<uint>(19200u, payloadMono8);
        Assert.AreEqual<uint>(38400u, payloadMono16);
    }

    [TestMethod]
    public async Task StartAcquisition_TransformsEnabled_RgbPixelFormatIsPreserved()
    {
        // Arrange
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", PixelFormat.RGB8.ToString());
        _device.Parameters.SetParameterValue("Width", "80");
        _device.Parameters.SetParameterValue("Height", "60");
        _device.Parameters.SetParameterValue("ReverseX", "True");

        // Act
        var buffer = await AcquireSingleBufferAsync(_device);

        // Assert
        Assert.AreEqual(PixelFormat.RGB8, buffer.PixelFormat);
    }

    [TestMethod]
    public async Task StartAcquisition_ImageDirectoryInvalidImage_RaisesAcquisitionAbortedAndStops()
    {
        // Arrange
        _device = new VirtualCam();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var invalidPngPath = Path.Combine(tempDir, "invalid.png");
        File.WriteAllText(invalidPngPath, "not-a-valid-image");

        _device.Parameters.SetParameterValue("TestPattern", TestPattern.ImageDirectory.ToString());
        _device.Parameters.SetParameterValue("ImageDirectory", tempDir);
        _device.Parameters.SetParameterValue("AcquisitionFrameRate", "60");

        var aborted = new TaskCompletionSource<AcquisitionAbortedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        void eventHandler(object s, AcquisitionAbortedEventArgs e) => aborted.TrySetResult(e);
        _device.AcquisitionAborted += eventHandler;

        try
        {
            // Act
            _device.StartAcquisition();
            var completedTask = await Task.WhenAny(aborted.Task, Task.Delay(3000, CancellationToken.None));

            // Assert
            Assert.AreSame(aborted.Task, completedTask, "Timed out waiting for AcquisitionAborted event.");
            Assert.IsNotNull(aborted.Task.Result);
            Assert.IsFalse(_device.IsAcquiring);
        }
        finally
        {
            _device.AcquisitionAborted -= eventHandler;
            _device.StopAcquisition();
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void StartStopAcquisition_RapidSequence_DoesNotLeaveAcquiringState()
    {
        // Arrange
        _device = new VirtualCam();

        // Act
        for (int i = 0; i < 25; i++)
        {
            _device.StartAcquisition();
            _device.StopAcquisition();
        }

        // Assert
        Assert.IsFalse(_device.IsAcquiring);
    }

    // Testing updates of parameter dependencies...

    [TestMethod]
    public void SetWidth_ValidateDependentParameters()
    {
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("Width", "400");
        GcInteger widthMax = _device.Parameters["WidthMax"] as GcInteger;
        GcInteger offsetX = _device.Parameters["OffsetX"] as GcInteger;

        Assert.AreEqual(widthMax.Value - 400, offsetX.Max);
    }

    [TestMethod]
    public void SetHeight_ValidateDependentParameters()
    {
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("Height", "300");
        GcInteger heightMax = _device.Parameters["HeightMax"] as GcInteger;
        GcInteger offsetY = _device.Parameters["OffsetY"] as GcInteger;

        Assert.AreEqual(heightMax.Value - 300, offsetY.Max);
    }

    [TestMethod]
    [DataRow(PixelFormat.Mono8)]
    [DataRow(PixelFormat.Mono10)]
    [DataRow(PixelFormat.Mono12)]
    [DataRow(PixelFormat.Mono14)]
    [DataRow(PixelFormat.Mono16)]
    [DataRow(PixelFormat.RGB8)]
    public void SetPixelFormat_ValidateDependentParameters(PixelFormat pixelFormat)
    {
        _device = new VirtualCam();
        _device.Parameters.SetParameterValue("PixelFormat", pixelFormat.ToString());

        GcEnumeration pixelSize = _device.Parameters["PixelSize"] as GcEnumeration;

        Assert.AreEqual(GenICamHelper.GetPixelSize(pixelFormat), (PixelSize)pixelSize.IntValue);
    }

    /// <summary>
    /// Starts acquisition on the specified device, waits for the first buffer event, and returns the received
    /// buffer.
    /// </summary>
    /// <remarks>Acquisition is stopped and the event handler is removed in all cases, including timeout or
    /// failure.</remarks>
    /// <param name="device">Virtual camera instance that raises buffer events during acquisition.</param>
    /// <param name="timeoutMs">Maximum time to wait, in milliseconds, for the first buffer event before timing out.</param>
    /// <returns>A task that completes with the first acquired <see cref="GcBuffer"/>.</returns>
    private static async Task<GcBuffer> AcquireSingleBufferAsync(VirtualCam device, int timeoutMs = 2000)
    {
        var firstBufferReceived = new TaskCompletionSource<NewBufferEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        void eventHandler(object s, NewBufferEventArgs e) => firstBufferReceived.TrySetResult(e);
        device.NewBuffer += eventHandler;

        try
        {
            device.StartAcquisition();
            var completedTask = await Task.WhenAny(firstBufferReceived.Task, Task.Delay(timeoutMs, CancellationToken.None));
            Assert.AreSame(firstBufferReceived.Task, completedTask, "Timed out waiting for NewBuffer event.");
            return firstBufferReceived.Task.Result.Buffer;
        }
        finally
        {
            device.StopAcquisition();
            device.NewBuffer -= eventHandler;
        }
    }

    /// <summary>
    /// Invokes a non-public instance method on the specified object by using reflection.
    /// </summary>
    /// <remarks>Uses BindingFlags.Instance and BindingFlags.NonPublic to find the method and asserts that a
    /// matching method is found before invocation.</remarks>
    /// <param name="instance">Object that contains the non-public instance method to invoke.</param>
    /// <param name="methodName">Name of the non-public instance method to locate and invoke.</param>
    /// <param name="arguments">Arguments to pass to the invoked method.</param>
    /// <returns>Result returned by the invoked method.</returns>
    private static object InvokeNonPublicMethod(object instance, string methodName, params object[] arguments)
    {
        var method = instance.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method, $"Method '{methodName}' was not found.");
        return method.Invoke(instance, arguments);
    }
}
