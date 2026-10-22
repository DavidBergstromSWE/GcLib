using System.Collections.Generic;

namespace GcLib.UnitTests.Utilities;

/// <summary>
/// Represents a device type for unit testing purposes. It simulates various failure modes to test error handling and recovery mechanisms in applications that interact with camera devices.
/// </summary>
/// <remarks>
/// The class can be configured to simulate various failure modes, such as enumeration failure, device creation failure, acquisition failure, parameter access failure and release failure. 
/// </remarks>
internal class FailingCam : GcDevice, IDeviceFactory<FailingCam>
{
    public enum FailureMode
    {
        None,
        EnumerationFailure,
        DeviceCreationFailure,
        AcquisitionFailure,
        ParameterAccessFailure,
        ReleaseFailure
    }

    public FailingCam()
    {
        if (CurrentFailureMode == FailureMode.DeviceCreationFailure)
            throw new System.Exception("Simulated device creation failure.");
    }

    public static FailureMode CurrentFailureMode { get; set; } = FailureMode.None;

    public static GcDeviceClassInfo DeviceClassInfo => new("UnavailableCam", "1.0", typeof(FailingCam));

    public override uint PayloadSize { get; }

    public override uint BufferCapacity { get; set; }

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
}
