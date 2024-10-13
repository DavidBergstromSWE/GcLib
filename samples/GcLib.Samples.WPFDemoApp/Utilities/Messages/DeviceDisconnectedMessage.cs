namespace FusionViewer.Utilities.Messages;

/// <summary>
/// A message announcing the disconnection of a device.
/// </summary>
/// <remarks>
/// Instantiates a new message announcing the disconnection of a device.
/// </remarks>
/// <param name="deviceIndex">Device index.</param>
internal sealed class DeviceDisconnectedMessage(DeviceIndex deviceIndex)
{

    /// <summary>
    /// Device index.
    /// </summary>
    public DeviceIndex DeviceIndex { get; } = deviceIndex;

    /// <summary>
    /// Image channel.
    /// </summary>
    public DisplayChannel DisplayChannel { get; } = deviceIndex == DeviceIndex.Device1 ? DisplayChannel.Channel1 : DisplayChannel.Channel2;
}