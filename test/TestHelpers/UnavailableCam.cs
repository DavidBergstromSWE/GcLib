using System.Collections.Generic;

namespace GcLib.UnitTests.Utilities;

/// <summary>
/// Represents a camera device that is unavailable (due to missing drivers or licensing issues) for unit testing purposes.
/// </summary>
internal class UnavailableCam : GcDevice, IDeviceEnumerator, IDeviceClassDescriptor
{
    public static GcDeviceClassInfo DeviceClassInfo => new("UnavailableCam", "1.0", typeof(UnavailableCam));

    public override uint PayloadSize => throw new System.NotImplementedException();

    public override uint BufferCapacity { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public static List<GcDeviceInfo> EnumerateDevices()
    {
        throw new System.NotImplementedException();
    }

    public override void StartAcquisition()
    {
        throw new System.NotImplementedException();
    }

    public override void StopAcquisition()
    {
        throw new System.NotImplementedException();
    }

    protected override void ExecuteParameterCommand(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override GcBoolean GetBoolean(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override GcCommand GetCommand(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override GcFloat GetFloat(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override GcInteger GetInteger(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override string GetParameterValue(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override GcString GetString(string parameterName)
    {
        throw new System.NotImplementedException();
    }

    protected override GcDeviceParameterCollection ImportParameters()
    {
        throw new System.NotImplementedException();
    }

    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        throw new System.NotImplementedException();
    }
}
