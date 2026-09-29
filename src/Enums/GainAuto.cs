namespace GcLib;

/// <summary>
/// Automatic gain control (AGC) mode.
/// </summary>
public enum GainAuto
{
    /// <summary>
    /// Gain is User controlled using Gain.
    /// </summary>
    Off,
    /// <summary>
    /// Gain is automatically adjusted once by the device. Once it has converged, it automatically returns to the Off state.
    /// </summary>
    Once,
    /// <summary>
    /// Gain is constantly adjusted by the device.
    /// </summary>
    Continuous
}
