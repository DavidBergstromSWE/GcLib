using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Events;

[TestClass]
public class EventArgsTests
{
    [TestMethod]
    public void ErrorEventArgs_ErrorMessageNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new ErrorEventArgs(null));
    }

    [TestMethod]
    public void NewBufferEventArgs_StreamIdNull_ThrowsArgumentNullException()
    {
        var buffer = new GcBuffer(new byte[1], 1, 1, PixelFormat.Mono8, 0, 0);
        Assert.Throws<ArgumentNullException>(() => _ = new NewBufferEventArgs(null, DateTime.UtcNow, buffer));
    }

    [TestMethod]
    public void NewBufferEventArgs_BufferNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new NewBufferEventArgs("s1", DateTime.UtcNow, null));
    }

    [TestMethod]
    public void BufferTransferredEventArgs_BufferNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new BufferTransferredEventArgs(null));
    }
}
