using System;
using System.Diagnostics;
using System.Threading.Tasks;
using GcLib.Utilities.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

[TestClass]
public class FPSStabilizerTests
{
    [TestMethod]
    public void Constructor_NumSamplesLessThanTwo_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new FPSStabilizer(1));
    }

    [TestMethod]
    public void Constructor_NullTimestampProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new FPSStabilizer(2, null));
    }

    [TestMethod]
    public void IsTimeToDisplay_BufferIsEmpty_ReturnsTrue()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(30, () => timestamp);

        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(60));
    }

    [TestMethod]
    public void Average_OneTimestamp_ReturnsZero()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(30, () => timestamp);

        fpsStabilizer.IsTimeToDisplay(60);

        Assert.AreEqual(0.0, fpsStabilizer.Average);
    }

    [TestMethod]
    public void Average_MultipleTimestamps_ReturnsFramesPerSecond()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(3, () => timestamp);
        fpsStabilizer.IsTimeToDisplay(60);

        timestamp = Stopwatch.Frequency / 10;
        fpsStabilizer.IsTimeToDisplay(60);
        timestamp = Stopwatch.Frequency / 5;
        fpsStabilizer.IsTimeToDisplay(60);

        Assert.AreEqual(10.0, fpsStabilizer.Average, 0.0001);
    }

    [TestMethod]
    public void IsTimeToDisplay_ProjectedRateEqualsTarget_ReturnsTrue()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(2, () => timestamp);
        fpsStabilizer.IsTimeToDisplay(10);

        timestamp = Stopwatch.Frequency / 10;

        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(10));
    }

    [TestMethod]
    public void IsTimeToDisplay_ProjectRateIncludesNewInterval()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(3, () => timestamp);
        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(60));

        timestamp = Stopwatch.Frequency / 100;
        Assert.IsFalse(fpsStabilizer.IsTimeToDisplay(60));

        timestamp = Stopwatch.Frequency / 10;
        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(60));
    }

    [TestMethod]
    public void IsTimeToDisplay_FullBufferUsesWindowAfterOldestIsEvicted()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(2, () => timestamp);
        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(2));

        timestamp = Stopwatch.Frequency;
        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(2));

        timestamp += Stopwatch.Frequency / 10;
        Assert.IsFalse(fpsStabilizer.IsTimeToDisplay(2));

        timestamp = 2 * Stopwatch.Frequency;
        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(2));
    }

    [TestMethod]
    public void Reset_AverageFPS_IsZero()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(30, () => timestamp);
        fpsStabilizer.IsTimeToDisplay(60);
        timestamp = Stopwatch.Frequency / 10;
        fpsStabilizer.IsTimeToDisplay(60);

        fpsStabilizer.Reset();

        Assert.AreEqual(0.0, fpsStabilizer.Average);
    }

    [TestMethod]
    public void Reset_IsTimeToDisplay_ReturnsTrue()
    {
        long timestamp = 0;
        var fpsStabilizer = new FPSStabilizer(30, () => timestamp);
        fpsStabilizer.IsTimeToDisplay(60);
        timestamp = Stopwatch.Frequency / 1000;

        fpsStabilizer.Reset();

        Assert.IsTrue(fpsStabilizer.IsTimeToDisplay(30));
    }

    [TestMethod]
    public void ConcurrentAccessToHistory_DoesNotThrow()
    {
        var fpsStabilizer = new FPSStabilizer();

        Parallel.For(0, 1000, iteration =>
        {
            switch (iteration % 3)
            {
                case 0:
                    fpsStabilizer.IsTimeToDisplay(60);
                    break;
                case 1:
                    _ = fpsStabilizer.Average;
                    break;
                default:
                    fpsStabilizer.Reset();
                    break;
            }
        });
    }
}