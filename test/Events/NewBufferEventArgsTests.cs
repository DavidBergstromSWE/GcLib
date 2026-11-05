using System;
using GcLib.UnitTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Events;

[TestClass]
public class NewBufferEventArgsTests
{
    [TestMethod]
    public void Constructor_ValidInput_AssignsProperties()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer();
        var receptionTime = DateTime.Now;
        var streamID = Guid.NewGuid().ToString();

        // Act
        var eventArgs = new NewBufferEventArgs(streamID,
                                               receptionTime,
                                               buffer.Width,
                                               buffer.Height,
                                               buffer.PixelFormat,
                                               buffer.FrameID,
                                               buffer.TimeStamp,
                                               buffer.ImageData.Length,
                                               destination => Buffer.BlockCopy(buffer.ImageData, 0, destination, 0, buffer.ImageData.Length));

        // Assert
        Assert.AreEqual(streamID, eventArgs.StreamID);
        Assert.AreEqual(receptionTime, eventArgs.ReceptionTime);
        Assert.AreEqual(buffer.Width, eventArgs.Width);
        Assert.AreEqual(buffer.Height, eventArgs.Height);
        Assert.AreEqual(buffer.PixelFormat, eventArgs.PixelFormat);
        Assert.AreEqual(buffer.FrameID, eventArgs.FrameID);
        Assert.AreEqual(buffer.TimeStamp, eventArgs.TimeStamp);
        Assert.AreEqual(buffer.ImageData.Length, eventArgs.PayloadSize);

        byte[] copied = new byte[buffer.ImageData.Length];
        eventArgs.CopyImageData(copied);
        CollectionAssert.AreEqual(buffer.ImageData, copied);
    }

    [TestMethod]
    public void Constructor_EmptyStreamID_PreservesStreamID()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer();

        // Act
        var eventArgs = new NewBufferEventArgs(string.Empty,
                                               DateTime.Now,
                                               buffer.Width,
                                               buffer.Height,
                                               buffer.PixelFormat,
                                               buffer.FrameID,
                                               buffer.TimeStamp,
                                               buffer.ImageData.Length,
                                               destination => Buffer.BlockCopy(buffer.ImageData, 0, destination, 0, buffer.ImageData.Length));

        // Assert
        Assert.AreEqual(string.Empty, eventArgs.StreamID);
    }
}
