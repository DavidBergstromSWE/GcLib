namespace GcLib;

public sealed partial class PcoCam
{
    /// <inheritdoc/>
    public override uint PayloadSize => _genApi.GetPayloadSize();

    /// <inheritdoc/>
    public override uint BufferCapacity
    {
        get => (uint)_genApi.InputBufferCount.Value;
        set => _genApi.InputBufferCount.Value = value;
    }

    /// <inheritdoc/>
    public override void StartAcquisition()
    {
        ExecuteParameterCommand("AcquisitionArm");
        ExecuteParameterCommand("AcquisitionStart");
    }

    /// <inheritdoc/>
    public override void StopAcquisition()
    {
        ExecuteParameterCommand("AcquisitionStop");
    }
}