using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Cyotek.Collections.Generic;

namespace GcLib.Utilities.Threading;

/// <summary>
/// Frame rate manager, used to stabilize frames per second.
/// </summary>
public sealed class FPSStabilizer
{
    #region Fields

    /// <summary>
    /// The capacity limit.
    /// </summary>
    private readonly int _capacity;

    /// <summary>
    /// Gets a function that retrieves the current timestamp.
    /// </summary>
    private readonly Func<long> _getTimestamp;

    /// <summary>
    /// Synchronizes access to shared state.
    /// </summary>
    private readonly Lock _sync = new();

    /// <summary>
    /// Circular buffer of timestamps measured by a monotonic clock.
    /// </summary>
    private readonly CircularBuffer<long> _timeStamps;

    #endregion

    /// <summary>
    /// Initializes a new frame rate manager.
    /// </summary>
    /// <param name="numSamples">Number of timestamps used to calculate average frame rate.</param>
    public FPSStabilizer(int numSamples = 30) : this(numSamples, Stopwatch.GetTimestamp)
    {
    }

    /// <summary>
    /// Initializes a new instance of the FPS stabilizer.
    /// </summary>
    /// <param name="numSamples">The number of timestamp samples to retain; must be at least two.</param>
    /// <param name="getTimestamp">A function that returns the current timestamp.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="numSamples"/> is less than two.</exception>
    internal FPSStabilizer(int numSamples, Func<long> getTimestamp)
    {
        if (numSamples < 2)
            throw new ArgumentOutOfRangeException(nameof(numSamples), "At least two samples are required.");

        ArgumentNullException.ThrowIfNull(getTimestamp);
        _capacity = numSamples;
        _getTimestamp = getTimestamp;
        _timeStamps = new CircularBuffer<long>(capacity: numSamples, allowOverwrite: true);
    }

    #region Properties

    /// <summary>
    /// Queries the average frames per seconds.
    /// </summary>
    public double Average
    {
        get
        {
            lock (_sync)
            {
                if (_timeStamps.Size < 2)
                    return 0.0; // Not enough samples to calculate average

                return CalcFPS(_timeStamps.Max(), _timeStamps.Min(), _timeStamps.Size - 1);
            }
        }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Checks whether an added frame would bring us closer to the targeted frame rate or not.
    /// </summary>
    /// <param name="targetFPS">The targeted (desired) frame rate.</param>
    /// <returns>True if image should be displayed, false if not.</returns>
    public bool IsTimeToDisplay(double targetFPS)
    {
        lock (_sync)
        {
            // If the buffer is empty, we should display the frame.
            long timeNow = _getTimestamp();
            if (_timeStamps.IsEmpty)
            {
                _timeStamps.Put(timeNow);
                return true;
            }

            // Determine if adding this timestamp would exceed the target FPS.
            bool willOverwrite = _timeStamps.Size == _capacity;
            long oldestTimestamp = willOverwrite ? GetSecondOldestTimestamp() : _timeStamps.Min();
            int intervals = willOverwrite ? _timeStamps.Size - 1 : _timeStamps.Size;

            // If the projected frame rate is less than or equal to the target, we should display the frame.
            if (CalcFPS(timeNow, oldestTimestamp, intervals) <= targetFPS)
            {
                _timeStamps.Put(timeNow);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Resets the timestamp history.
    /// </summary>
    public void Reset()
    {
        lock (_sync)
            _timeStamps.Clear();
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Calculates the frame rate over the specified number of intervals.
    /// </summary>
    /// <param name="latestTimestamp">The latest timestamp, in stopwatch ticks.</param>
    /// <param name="oldestTimestamp">The oldest timestamp, in stopwatch ticks.</param>
    /// <param name="intervals">The number of intervals between the timestamps.</param>
    /// <returns>The frame rate in frames per second, or zero when <paramref name="intervals"/> is nonpositive. Returns positive
    /// infinity when the elapsed time is nonpositive.</returns>
    private static double CalcFPS(long latestTimestamp, long oldestTimestamp, int intervals)
    {
        // Calculate the elapsed time in stopwatch ticks.
        long elapsed = latestTimestamp - oldestTimestamp;
        if (intervals <= 0)
            return 0.0;
        if (elapsed <= 0)
            return double.PositiveInfinity;

        return intervals * (double)Stopwatch.Frequency / elapsed; // Convert to frames per second
    }

    /// <summary>
    /// Gets the second-oldest timestamp, counting duplicate values.
    /// </summary>
    /// <returns>The second-oldest timestamp, or <see cref="long.MaxValue"/> if fewer than two timestamps are available.</returns>
    private long GetSecondOldestTimestamp()
    {
        // Find the oldest and second-oldest timestamps in the circular buffer, counting duplicates.
        long oldestTimestamp = long.MaxValue;
        long secondOldestTimestamp = long.MaxValue;
        bool hasOldestTimestamp = false;

        // Loop through the timestamps to find the oldest and second-oldest values.
        foreach (long timestamp in _timeStamps)
        {
            if (!hasOldestTimestamp || timestamp < oldestTimestamp)
            {
                secondOldestTimestamp = oldestTimestamp;
                oldestTimestamp = timestamp;
                hasOldestTimestamp = true;
            }
            else if (timestamp < secondOldestTimestamp)
            {
                secondOldestTimestamp = timestamp;
            }
        }

        return secondOldestTimestamp;
    }

    #endregion
}