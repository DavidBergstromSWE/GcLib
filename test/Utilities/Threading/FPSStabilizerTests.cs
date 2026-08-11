using GcLib.Utilities.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests;

[TestClass]
public class FPSStabilizerTests
{
    [TestMethod]
    public void IsTimeToDisplay_BufferIsEmpty_ReturnsTrue()
    {
        // Arrange
        var fpsStabilizer = new FPSStabilizer();

        // Act
        bool result = fpsStabilizer.IsTimeToDisplay(60);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTimeToDisplay_AverageFPSLessThanTarget_ReturnsTrue()
    {
        // Arrange
        var fpsStabilizer = new FPSStabilizer();
        for (int i = 0; i < 30; i++)
        {
            fpsStabilizer.IsTimeToDisplay(30); // Fill the buffer with frames
            System.Threading.Thread.Sleep(20); // Simulate frame delay
        }

        // Act
        bool result = fpsStabilizer.IsTimeToDisplay(60);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void IsTimeToDisplay_AverageFPSGreaterThanTarget_ReturnsFalse()
    {
        // Arrange
        var fpsStabilizer = new FPSStabilizer();
        for (int i = 0; i < 30; i++)
        {
            fpsStabilizer.IsTimeToDisplay(60); // Fill the buffer with frames
            System.Threading.Thread.Sleep(10); // Simulate frame delay
        }

        // Act
        bool result = fpsStabilizer.IsTimeToDisplay(30); // Target FPS is lower than average

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void Reset_AverageFPS_IsZero()
    {
        // Arrange
        var fpsStabilizer = new FPSStabilizer();
        for (int i = 0; i < 30; i++)
        {
            fpsStabilizer.IsTimeToDisplay(60); // Fill the buffer with frames
            System.Threading.Thread.Sleep(10); // Simulate frame delay
        }

        // Act
        fpsStabilizer.Reset();

        // Assert
        Assert.AreEqual(0, fpsStabilizer.Average);
    }

    [TestMethod]
    public void Reset_IsTimeToDisplay_ReturnsTrue()
    {
        // Arrange
        var fpsStabilizer = new FPSStabilizer();
        for (int i = 0; i < 30; i++)
        {
            fpsStabilizer.IsTimeToDisplay(60); // Fill the buffer with frames
            System.Threading.Thread.Sleep(10); // Simulate frame delay
        }

        // Act
        fpsStabilizer.Reset();
        bool result = fpsStabilizer.IsTimeToDisplay(30);

        // Assert
        Assert.IsTrue(result); // After reset, buffer is empty, so it should return true
    }
}