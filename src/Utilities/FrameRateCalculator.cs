using System;

namespace GcLib.Utilities;

/// <summary>
/// Calculates frame rates from elapsed clock ticks.
/// </summary>
public static class FrameRateCalculator
{
    /// <summary>
    /// Calculates frames per second over a timestamp interval.
    /// </summary>
    /// <param name="elapsedTicks">Elapsed ticks between the first and last frame.</param>
    /// <param name="intervals">Number of frame intervals between those timestamps.</param>
    /// <param name="ticksPerSecond">Number of clock ticks per second for the timestamp source.</param>
    /// <returns>The frame rate, or zero when the interval, elapsed time, or clock frequency is invalid.</returns>
    public static double Calculate(double elapsedTicks, int intervals, double ticksPerSecond)
    {
        if (intervals <= 0 ||
            !double.IsFinite(elapsedTicks) || elapsedTicks <= 0 ||
            !double.IsFinite(ticksPerSecond) || ticksPerSecond <= 0)
            return 0.0;

        return intervals * ticksPerSecond / elapsedTicks;
    }
}
