using System.Collections.Generic;

namespace GcLib.UnitTests.Utilities;

/// <summary>
/// Represents a device type for unit testing purposes. It simulates various failure modes to test error handling and recovery mechanisms in applications that interact with camera devices.
/// </summary>
/// <remarks>
/// The class can be configured to simulate various failure modes, such as failure occuring during device enumeration, device creation, acquisition, parameter access and release operations. 
/// </remarks>
internal class FailingCam : GcDevice<FailingCam>, IDeviceFactory<FailingCam>
{
    #region Enums

    /// <summary>
    /// Represents the outcome of an operation by identifying the stage where a failure occurred.
    /// </summary>
    /// <remarks>Use these values to classify and report failures consistently across device enumeration,
    /// creation, acquisition, parameter access, and release operations.</remarks>
    public enum FailureMode
    {
        /// <summary>
        /// No failure occurred.
        /// </summary>
        None,
        /// <summary>
        /// Indicates that initialization failed.
        /// </summary>
        InitializationFailure,
        /// <summary>
        /// Represents a failure that occurred while enumerating devices.
        /// </summary>
        EnumerationFailure,
        /// <summary>
        /// Indicates that device creation failed.
        /// </summary>
        DeviceCreationFailure,
        /// <summary>
        /// Indicates that an acquisition operation failed.
        /// </summary>
        AcquisitionFailure,
        /// <summary>
        /// Indicates that an attempt to access a parameter failed.
        /// </summary>
        ParameterAccessFailure,
        /// <summary>
        /// Represents a failed release operation.
        /// </summary>
        ReleaseFailure
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the current failure mode.
    /// </summary>
    public static FailureMode CurrentFailureMode { get; set; } = FailureMode.None;

    public static GcDeviceClassInfo DeviceClassInfo => new("FailingCam", "1.0", typeof(FailingCam));

    public override uint PayloadSize { get; }

    public override uint BufferCapacity { get; set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="FailingCam"/> class.
    /// </summary>
    /// <exception cref="System.Exception">Thrown when <see cref="CurrentFailureMode"/> is <see cref="FailureMode.DeviceCreationFailure"/>.</exception>
    public FailingCam()
    {
        if (CurrentFailureMode == FailureMode.DeviceCreationFailure)
            throw new System.Exception("Simulated device creation failure.");
    }

    #endregion

    #region Public methods

    public override void StartAcquisition()
    {
        if (CurrentFailureMode == FailureMode.AcquisitionFailure)
            throw new System.Exception("Simulated acquisition failure.");
    }

    public override void StopAcquisition()
    {
        if (CurrentFailureMode == FailureMode.AcquisitionFailure)
            throw new System.Exception("Simulated acquisition failure.");
    }

    #endregion

    #region Protected methods

    protected override void ExecuteParameterCommand(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
    }

    protected override GcBoolean GetBoolean(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return new GcBoolean(parameterName);
    }

    protected override GcCommand GetCommand(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return new GcCommand(parameterName);
    }

    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return new GcEnumeration(parameterName);
    }

    protected override GcFloat GetFloat(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return new GcFloat(parameterName);
    }

    protected override GcInteger GetInteger(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return new GcInteger(parameterName);
    }

    protected override GcString GetString(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return new GcString(parameterName);
    }

    protected override string GetParameterValue(string parameterName)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return parameterName;
    }

    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
    }

    protected override GcDeviceParameterCollection ImportParameters()
    {
        if (CurrentFailureMode == FailureMode.ParameterAccessFailure)
            throw new System.Exception("Simulated parameter access failure.");
        else return new GcDeviceParameterCollection(this, [], []);
    }

    protected override void OnDeviceClosed()
    {
        // No special cleanup required for this simulated device.
        CurrentFailureMode = FailureMode.None; // Reset failure mode on close for testing purposes.
    }

    #endregion

    #region IDeviceFactory

    public static void Initialize()
    {
        // No specific initialization required for FailingCam.
        if (CurrentFailureMode == FailureMode.InitializationFailure)
            throw new System.Exception("Simulated initialization failure.");
    }

    public static FailingCam CreateDevice(GcDeviceInfo deviceInfo)
    {
        if (CurrentFailureMode == FailureMode.DeviceCreationFailure)
            throw new System.Exception("Simulated device creation failure.");
        else return new FailingCam();
    }

    public static List<GcDeviceInfo> EnumerateDevices()
    {
        if (CurrentFailureMode == FailureMode.EnumerationFailure)
            throw new System.Exception("Simulated enumeration failure.");
        else
        {
            var devices = new List<GcDeviceInfo>();
            for (uint i = 1; i <= 3; i++)
                devices.Add(new GcDeviceInfo("GcLib", "FailingCam", "1.0", $"FailingCam{i}", DeviceClassInfo));
            return devices;
        }
    }

    static void IDeviceFactory<FailingCam>.Release()
    {
        if (CurrentFailureMode == FailureMode.ReleaseFailure)
            throw new System.Exception("Simulated release failure.");
    }

    #endregion
}
