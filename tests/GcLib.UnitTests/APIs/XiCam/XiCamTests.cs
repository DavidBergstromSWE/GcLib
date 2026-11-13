using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using xiApi;
using xiApi.NET;

namespace GcLib.UnitTests.APIs;

[TestClass]
public class XiCamTests
{
    [TestMethod]
    [DataRow(IMG_FORMAT.MONO8, PixelFormat.Mono8)]
    [DataRow(IMG_FORMAT.MONO16, PixelFormat.Mono16)]
    [DataRow(IMG_FORMAT.RGB24, PixelFormat.BGR8)]
    [DataRow(IMG_FORMAT.RGB32, PixelFormat.BGRa8)]
    [DataRow(IMG_FORMAT.RGB48, PixelFormat.BGR16)]
    [DataRow(IMG_FORMAT.RGB64, PixelFormat.BGRa16)]
    [DataRow(IMG_FORMAT.RGBPLANAR, PixelFormat.RGB8_Planar)]
    [DataRow(IMG_FORMAT.RGB16_PLANAR, PixelFormat.RGB16_Planar)]
    public void GetPixelFormat_SupportedFormat_ReturnsPfncFormat(int format, PixelFormat expected)
    {
        Assert.AreEqual(expected, GetPixelFormat(format));
    }

    [TestMethod]
    [DataRow(IMG_FORMAT.RAW8, IMG_COLOR_FILTER_ARRAY.BAYER_RGGB, PixelFormat.BayerRG8)]
    [DataRow(IMG_FORMAT.RAW8, IMG_COLOR_FILTER_ARRAY.BAYER_BGGR, PixelFormat.BayerBG8)]
    [DataRow(IMG_FORMAT.RAW8, IMG_COLOR_FILTER_ARRAY.BAYER_GRBG, PixelFormat.BayerGR8)]
    [DataRow(IMG_FORMAT.RAW8, IMG_COLOR_FILTER_ARRAY.BAYER_GBRG, PixelFormat.BayerGB8)]
    [DataRow(IMG_FORMAT.RAW16, IMG_COLOR_FILTER_ARRAY.BAYER_RGGB, PixelFormat.BayerRG16)]
    [DataRow(IMG_FORMAT.RAW16, IMG_COLOR_FILTER_ARRAY.BAYER_BGGR, PixelFormat.BayerBG16)]
    [DataRow(IMG_FORMAT.RAW16, IMG_COLOR_FILTER_ARRAY.BAYER_GRBG, PixelFormat.BayerGR16)]
    [DataRow(IMG_FORMAT.RAW16, IMG_COLOR_FILTER_ARRAY.BAYER_GBRG, PixelFormat.BayerGB16)]
    [DataRow(IMG_FORMAT.RAW8, IMG_COLOR_FILTER_ARRAY.NONE, PixelFormat.InvalidPixelFormat)]
    [DataRow(IMG_FORMAT.RAW16, IMG_COLOR_FILTER_ARRAY.CMYG, PixelFormat.InvalidPixelFormat)]
    public void GetPixelFormat_RawFormat_UsesColorFilter(int format, int filter, PixelFormat expected)
    {
        Assert.AreEqual(expected, GetPixelFormat(format, filter));
    }

    [TestMethod]
    [DataRow(IMG_FORMAT.FRM_TRANSPORT_DATA)]
    [DataRow(IMG_FORMAT.RAW8X2)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    public void GetPixelFormat_UnsupportedFormat_ReturnsInvalid(int format)
    {
        Assert.AreEqual(PixelFormat.InvalidPixelFormat, GetPixelFormat(format));
    }

    [TestMethod]
    public void FindConstantName_MatchesPublicConstantAndType()
    {
        Assert.AreEqual(nameof(IMG_FORMAT.RGB24), FindConstantName(typeof(IMG_FORMAT), IMG_FORMAT.RGB24));
        Assert.IsNull(FindConstantName(typeof(IMG_FORMAT), int.MaxValue));
        Assert.IsNull(FindConstantName(typeof(IMG_FORMAT), (long)IMG_FORMAT.RGB24));
    }

    [TestMethod]
    [DataRow("<Enumeration/>", new string[0])]
    [DataRow("<Enumeration><pSelected>Width</pSelected></Enumeration>", new[] { "Width" })]
    [DataRow("<Enumeration><pSelected>Width</pSelected><pSelected>Height</pSelected></Enumeration>", new[] { "Width", "Height" })]
    public void GetSelectedParameters_ReturnsAllDependencies(string xml, string[] expected)
    {
        var document = new XmlDocument();
        document.LoadXml(xml);
        var method = typeof(XiCam).GetMethod("GetSelectedParameters", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var selected = (List<string>)method.Invoke(null, [document.DocumentElement]);

        CollectionAssert.AreEqual(expected, selected);
    }

    [TestMethod]
    [DataRow(XiCam.BUFFERPOLICY.SAFE)]
    [DataRow(XiCam.BUFFERPOLICY.UNSAFE)]
    public void ToNewBufferEventArgs_PreservesMetadataAndBufferOwnership(XiCam.BUFFERPOLICY policy)
    {
        var device = (XiCam)RuntimeHelpers.GetUninitializedObject(typeof(XiCam));
        SetField(device, "_bufferPolicy", policy);
        SetField(device, "_pcTime0", 100000000UL);
        SetField(device, "_acquisitionStartTime", 1000000000UL);
        var imageParams = new ImgParams();
        var updateValues = typeof(ImgParams).GetMethod("UpdateValues", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(updateValues);
        updateValues.Invoke(imageParams, [new XI_IMG
        {
            width = 2,
            height = 2,
            frm = XI_IMG_FORMAT.MONO8,
            nframe = 42,
            tsSec = 2,
            tsUSec = 500000,
            padding_x = 1
        }]);
        byte[] source = [1, 2, 0, 3, 4, 0];
        var method = typeof(XiCam).GetMethod("ToNewBufferEventArgs", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var result = (NewBufferEventArgs)method.Invoke(device, [source, imageParams]);

        Assert.AreEqual(string.Empty, result.StreamID);
        Assert.AreEqual(2U, result.Buffer.Width);
        Assert.AreEqual(2U, result.Buffer.Height);
        Assert.AreEqual(PixelFormat.Mono8, result.Buffer.PixelFormat);
        Assert.AreEqual(42L, result.Buffer.FrameID);
        Assert.AreEqual(115000000UL, result.Buffer.TimeStamp);
        Assert.AreEqual(1U, result.Buffer.PaddingX);
        CollectionAssert.AreEqual(source, result.Buffer.ImageData);
        if (policy == XiCam.BUFFERPOLICY.SAFE)
            Assert.AreSame(source, result.Buffer.ImageData);
        else
        {
            Assert.AreNotSame(source, result.Buffer.ImageData);
            source[0] = 99;
            Assert.AreEqual((byte)1, result.Buffer.ImageData[0]);
        }
    }

    [TestMethod]
    [DataRow(XI_COLOR_FILTER_ARRAY.CFA_BAYER_RGGB, PixelFormat.BayerRG8)]
    [DataRow(XI_COLOR_FILTER_ARRAY.CFA_BAYER_BGGR, PixelFormat.BayerBG8)]
    [DataRow(XI_COLOR_FILTER_ARRAY.CFA_BAYER_GRBG, PixelFormat.BayerGR8)]
    [DataRow(XI_COLOR_FILTER_ARRAY.CFA_BAYER_GBRG, PixelFormat.BayerGB8)]
    public void ToNewBufferEventArgs_RawFrame_UsesFrameColorFilter(XI_COLOR_FILTER_ARRAY filter, PixelFormat expected)
    {
        var device = (XiCam)RuntimeHelpers.GetUninitializedObject(typeof(XiCam));
        SetField(device, "_bufferPolicy", XiCam.BUFFERPOLICY.SAFE);
        var imageParams = new ImgParams();
        var updateValues = typeof(ImgParams).GetMethod("UpdateValues", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(updateValues);
        updateValues.Invoke(imageParams, [new XI_IMG
        {
            width = 2,
            height = 2,
            frm = XI_IMG_FORMAT.RAW8,
            color_filter_array = filter
        }]);
        var method = typeof(XiCam).GetMethod("ToNewBufferEventArgs", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(method);

        var result = (NewBufferEventArgs)method.Invoke(device, [new byte[4], imageParams]);

        Assert.AreEqual(expected, result.Buffer.PixelFormat);
    }

    [TestMethod]
    public void StartAcquisition_WhenAlreadyAcquiring_ThrowsWithoutRaisingEvent()
    {
        var device = (XiCam)RuntimeHelpers.GetUninitializedObject(typeof(XiCam));
        typeof(GcDevice).GetProperty(nameof(GcDevice.DeviceInfo)).SetValue(
            device, new GcDeviceInfo("Ximea", "Test camera", "serial", "id", null));
        typeof(GcDevice).GetProperty(nameof(GcDevice.IsAcquiring)).SetValue(device, true);
        int startedCount = 0;
        device.AcquisitionStarted += (_, _) => startedCount++;

        var exception = Assert.Throws<InvalidOperationException>(() => device.StartAcquisition());

        StringAssert.Contains(exception.Message, "Test camera");
        Assert.IsTrue(device.IsAcquiring);
        Assert.AreEqual(0, startedCount);
    }

    [TestMethod]
    public void StartAcquisition_PreviousWorkerStillRunning_ThrowsBeforeAccessingCamera()
    {
        var device = (XiCam)RuntimeHelpers.GetUninitializedObject(typeof(XiCam));
        using var releaseWorker = new ManualResetEventSlim();
        var worker = new Thread(() => releaseWorker.Wait());
        SetField(device, "_imageAcquisitionThread", worker);
        worker.Start();
        try
        {
            var exception = Assert.Throws<InvalidOperationException>(() => device.StartAcquisition());
            StringAssert.Contains(exception.Message, "previous acquisition thread");
        }
        finally
        {
            releaseWorker.Set();
            worker.Join();
        }
    }

    [TestMethod]
    public void StopAcquisition_WhenNotAcquiring_DoesNotRaiseStoppedEvent()
    {
        var device = (XiCam)RuntimeHelpers.GetUninitializedObject(typeof(XiCam));
        int stoppedCount = 0;
        device.AcquisitionStopped += (_, _) => stoppedCount++;

        device.StopAcquisition();

        Assert.IsFalse(device.IsAcquiring);
        Assert.AreEqual(0, stoppedCount);
    }

    private static PixelFormat GetPixelFormat(int format, int filter = IMG_COLOR_FILTER_ARRAY.NONE)
    {
        var method = typeof(XiCam).GetMethod("GetPixelFormat", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (PixelFormat)method.Invoke(null, [format, filter]);
    }

    private static string FindConstantName<T>(Type type, T value)
    {
        var method = typeof(XiCam).GetMethod("FindConstantName", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.IsNotNull(method);
        return (string)method.MakeGenericMethod(typeof(T)).Invoke(null, [type, value]);
    }

    private static void SetField(XiCam device, string name, object value)
    {
        var field = typeof(XiCam).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.IsNotNull(field);
        field.SetValue(device, value);
    }
}
