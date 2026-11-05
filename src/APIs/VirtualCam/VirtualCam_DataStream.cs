namespace GcLib;

public sealed partial class VirtualCam
{
    /// <inheritdoc/>
    public override uint PayloadSize => (uint)_genApi.PayloadSize.Value;

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        ExecuteParameterCommand("AcquisitionStart");
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        ExecuteParameterCommand("AcquisitionStop");
    }
}