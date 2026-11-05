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
        var eventArgs = new NewBufferEventArgs(buffer, receptionTime, streamID);

        // Assert
        Assert.AreSame(buffer, eventArgs.Buffer);
        Assert.AreEqual(receptionTime, eventArgs.ReceptionTime);
        Assert.AreEqual(streamID, eventArgs.StreamID);
    }

    [TestMethod]
    public void Constructor_EmptyStreamID_PreservesStreamID()
    {
        // Arrange
        var buffer = FakeBufferProvider.GetFakeBuffer();

        // Act
        var eventArgs = new NewBufferEventArgs(buffer, DateTime.Now, string.Empty);

        // Assert
        Assert.AreEqual(string.Empty, eventArgs.StreamID);
    }
}
