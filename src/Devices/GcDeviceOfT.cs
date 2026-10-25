namespace GcLib;

/// <summary>
/// Generic base class for devices, applying the curiously recurring template pattern (CRTP). The derived type
/// must implement the static members of <see cref="IDeviceFactory{TDevice}"/>, which are then accessible from the base class.
/// </summary>
/// <typeparam name="TDevice">The derived device type itself.</typeparam>
public abstract class GcDevice<TDevice> : GcDevice where TDevice : GcDevice<TDevice>, IDeviceFactory<TDevice>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GcDevice{TDevice}"/> class.
    /// </summary>
    protected GcDevice() : base()
    {

    }
}
