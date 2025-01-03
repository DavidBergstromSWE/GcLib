﻿using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace GcLib.UnitTests
{
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
            _device = null;
        }

        [TestMethod]
        public void EnumerateDevices_CountIsTwo()
        {
            // Act
            var devices = VirtualCam.EnumerateDevices();

            // Assert
            Assert.IsTrue(devices.Count == 2);
        }

        [TestMethod]
        public void VirtualCam_ValidateInstantiation()
        {
            // Arrange
            var ID = "TestCam";

            // Act
            _device = new VirtualCam(ID);

            // Assert
            Assert.IsTrue(VirtualCam.DeviceConnectionDelay == 0);
            Assert.IsTrue(_device.PayloadSize > 0);
            Assert.IsTrue(_device.Parameters.Count > 0);
            Assert.IsTrue(_device.BufferCapacity > 0);
            Assert.IsFalse(_device.IsAcquiring);         
            Assert.IsNotNull(_device.DeviceInfo);
            Assert.IsTrue(_device.DeviceInfo.IsOpen);
            Assert.IsFalse(_device.DeviceInfo.IsAccessible);
            Assert.IsTrue(_device.DeviceInfo.ModelName == nameof(VirtualCam));
            Assert.IsNotNull(_device.DeviceInfo.SerialNumber);
            Assert.IsTrue(_device.DeviceInfo.UniqueID == ID);
            Assert.IsTrue(_device.DeviceInfo.UserDefinedName == ID);
            Assert.IsTrue(_device.DeviceInfo.DeviceClassInfo.DeviceType == typeof(VirtualCam));
            Assert.IsTrue(_device.GetNumDataStreams() == 0);
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
            Assert.IsTrue(eventCounter == 1);

            // Cleanup
            _device.AcquisitionStarted -= eventHandler;
        }

        [TestMethod]
        public async Task StartAcquisition_NewBufferEventIsRaised()
        {
            // Arrange
            _device = new VirtualCam();
            int eventCounter = 0;
            void eventHandler(object s, EventArgs e) { eventCounter++; }
            _device.NewBuffer += eventHandler;

            // Act
            _device.StartAcquisition();
            await Task.Delay(100);

            // Assert
            Assert.IsTrue(eventCounter > 0);

            // Cleanup
            _device.NewBuffer -= eventHandler;
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
            Assert.IsTrue(eventCounter == 1);

            // Cleanup
            _device.AcquisitionStopped -= eventHandler;
        }

        [TestMethod]
        public void StopAcquisition_IsRunning_IsNotRunning()
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
            Assert.IsTrue(eventCounter == 0);

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
            Assert.IsTrue(actualTypeName == expectedTypeName);
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
        [DataRow("TestFloat", "2,7")]
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
            Assert.IsTrue(actualValue == expectedValue);
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
            Assert.IsTrue(eventCounter == 1);

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
            Assert.IsTrue(eventCounter == 1);

            // Cleanup
            _device.ParameterInvalidate -= eventHandler;
        }

        // Add tests for FailedBuffer, ConnectionLost, AcquisitionAborted?

        // Testing updates of parameter dependencies...

        [TestMethod]
        public void SetWidth_ValidateDependentParameters()
        {
            _device = new VirtualCam();
            _device.Parameters.SetParameterValue("Width", "400");
            GcInteger widthMax = _device.Parameters["WidthMax"] as GcInteger;
            GcInteger offsetX = _device.Parameters["OffsetX"] as GcInteger;

            Assert.IsTrue(offsetX.Max == widthMax.Value - 400);
        }

        [TestMethod]
        public void SetHeight_ValidateDependentParameters()
        {
            _device = new VirtualCam();
            _device.Parameters.SetParameterValue("Height", "300");
            GcInteger heightMax = _device.Parameters["HeightMax"] as GcInteger;
            GcInteger offsetY = _device.Parameters["OffsetY"] as GcInteger;

            Assert.IsTrue(offsetY.Max == heightMax.Value - 300);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(4)]
        public void SetBinningHorizontal_ValidateDependentParameters(int binningHorizontal)
        {
            _device = new VirtualCam();
            _device.Parameters.SetParameterValue("BinningHorizontal", binningHorizontal.ToString());

            GcInteger width = _device.Parameters["Width"] as GcInteger;
            GcInteger widthMax = _device.Parameters["WidthMax"] as GcInteger;
            GcInteger offsetX = _device.Parameters["OffsetX"] as GcInteger;

            Assert.IsTrue(width == 320 / binningHorizontal);
            Assert.IsTrue(widthMax == 640 / binningHorizontal);
            Assert.IsTrue(offsetX.Max == widthMax - width);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(2)]
        [DataRow(4)]
        public void SetBinningVertical_ValidateDependentParameters(int binningVertical)
        {
            _device = new VirtualCam();
            _device.Parameters.SetParameterValue("BinningVertical", binningVertical.ToString());

            GcInteger height = _device.Parameters["Height"] as GcInteger;
            GcInteger heightMax = _device.Parameters["HeightMax"] as GcInteger;
            GcInteger offsetY = _device.Parameters["OffsetY"] as GcInteger;

            Assert.IsTrue(height == 240 / binningVertical);
            Assert.IsTrue(heightMax == 480 / binningVertical);
            Assert.IsTrue(offsetY.Max == heightMax - height);
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

            Assert.IsTrue((PixelSize)pixelSize.IntValue == GenICamConverter.GetPixelSize(pixelFormat));
        }
    }
}
