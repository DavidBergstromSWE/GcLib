using GcLib.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

[TestClass]
public class FrameRateCalculatorTests
{
    [TestMethod]
    public void Calculate_ValidInterval_ReturnsFramesPerSecond()
    {
        double actual = FrameRateCalculator.Calculate(100, 3, 1_000);

        Assert.AreEqual(30.0, actual);
    }

    [TestMethod]
    [DataRow(0.0, 1, 10_000_000.0)]
    [DataRow(-1.0, 1, 10_000_000.0)]
    [DataRow(10_000_000.0, 0, 10_000_000.0)]
    [DataRow(10_000_000.0, -1, 10_000_000.0)]
    [DataRow(10_000_000.0, 1, 0.0)]
    [DataRow(10_000_000.0, 1, -1.0)]
    public void Calculate_InvalidInput_ReturnsZero(double elapsedTicks, int intervals, double ticksPerSecond)
    {
        Assert.AreEqual(0.0, FrameRateCalculator.Calculate(elapsedTicks, intervals, ticksPerSecond));
    }
}
