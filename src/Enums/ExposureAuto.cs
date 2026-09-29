namespace GcLib;

/// <summary>
/// Automatic exposure modes.
/// </summary>
public enum ExposureAuto
{
    /// <summary>
    /// Exposure duration is user controlled using ExposureTime.
    /// </summary>
    Off,
    /// <summary>
    /// Exposure duration is adapted once by the device. Once it has converged, it returns to the Off state.
    /// </summary>
    Once,
    /// <summary>
    /// Exposure duration is constantly adapted by the device to maximize the dynamic range.
    /// </summary>
    Continuous
}
