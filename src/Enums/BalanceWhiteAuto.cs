namespace GcLib;

/// <summary>
/// Mode for automatic white balancing between the color channels.
/// </summary>
public enum BalanceWhiteAuto
{
    /// <summary>
    /// White balancing is user controlled using BalanceRatioSelector and BalanceRatio.
    /// </summary>
    Off,
    /// <summary>
    /// White balancing is automatically adjusted once by the device. Once it has converged, it automatically returns to the Off state.
    /// </summary>
    Once,
    /// <summary>
    /// White balancing is constantly adjusted by the device.
    /// </summary>
    Continuous
}
