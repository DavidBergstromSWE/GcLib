using System;
using System.Linq;
using GcLib.Utilities.Collections;

namespace GcLib.Utilities.Threading;

/// <summary>
/// Frame rate manager, used to stabilize frames per second.
/// </summary>
/// <remarks>
/// Initializes a new frame rate manager.
/// </remarks>
/// <param name="numSamples">Number of samples used to calculate average frame rate.</param>
public sealed class FPSStabilizer(int numSamples = 30)
{
    #region Fields

    /// <summary>
    /// Circular buffer of timestamps.
    /// </summary>
    private readonly CircularBuffer<long> _timeStamps = new(numSamples, true);

    #endregion

    #region Properties

    /// <summary>
    /// Queries the average frames per seconds.
    /// </summary>
    public double Average => (_timeStamps.IsEmpty == false) ? TimeSpan.TicksPerSecond / (_timeStamps.Max() - (double)_timeStamps.Min()) * (_timeStamps.Size - 1) : 0.0;

    #endregion

    #region Public methods

    /// <summary>
    /// Checks whether an added frame would bring us closer to the targeted frame rate or not. 
    /// </summary>
    /// <param name="targetFPS">The targeted (desired) frame rate.</param>
    /// <returns>True if image should be displayed, false if not.</returns>
    public bool IsTimeToDisplay(double targetFPS)
    {
        long timeNow = DateTime.Now.Ticks;

        if (_timeStamps.IsEmpty)
        {
            _timeStamps.Put(timeNow);
            return true;
        }

        long[] array = [.. _timeStamps.Where(x => x > 0)];
        if (TimeSpan.TicksPerSecond / (timeNow - (double)array.Min()) * array.Length <= targetFPS)
        {
            _timeStamps.Put(timeNow);
            return true;
        }
        else return false;
    }

    /// <summary>
    /// Resets the timestamp history.
    /// </summary>
    public void Reset()
    {
        _timeStamps.Clear();
    }

    #endregion
}