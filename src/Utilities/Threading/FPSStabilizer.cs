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
    private readonly CircularBuffer<long> _timeStamps = new(capacity: numSamples, allowOverflow: true);

    #endregion

    #region Properties

    /// <summary>
    /// Queries the average frames per seconds.
    /// </summary>
    public double Average => (_timeStamps.IsEmpty == false) ? CalcFPS(_timeStamps.Max()) : 0.0;

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

        // If the buffer is empty or if the new average FPS will be less than or equal to the target FPS, we can display the frame.
        if (_timeStamps.IsEmpty || CalcFPS(timeNow) <= targetFPS)
        {
            _timeStamps.Put(timeNow);
            return true;
        }
        else return false; // Otherwise, we skip the frame to maintain the target FPS.
    }

    /// <summary>
    /// Resets the timestamp history.
    /// </summary>
    public void Reset()
    {
        _timeStamps.Clear();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Calculates the average frames per second based on the current timestamps in the buffer and the added one.
    /// </summary>
    /// <param name="timeStamp">Added timestamp.</param>
    /// <returns>The calculated frames per second.</returns>
    private double CalcFPS(long timeStamp)
    {
        return (_timeStamps.Size - 1) / ((double)timeStamp - _timeStamps.Min()) * TimeSpan.TicksPerSecond;
    }

    #endregion
}