using System;
using GcLib.UnitTests.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Events;

[TestClass]
public class NewBufferEventArgsTests
{
    [TestMethod]
    public void Constructor_DirectPayload_AssignsProperties()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer();
        var receptionTime = DateTime.Now;
        var streamID = Guid.NewGuid().ToString();

        // Act
        var eventArgs = new NewBufferEventArgs(streamID,
                                               receptionTime,
                                               buffer);

        // Assert
        Assert.AreEqual(streamID, eventArgs.StreamID);
        Assert.AreEqual(receptionTime, eventArgs.ReceptionTime);
        Assert.AreSame(buffer, eventArgs.Buffer);
        Assert.IsTrue(eventArgs.Buffer.ImageData.Length > 0);
    }
}
