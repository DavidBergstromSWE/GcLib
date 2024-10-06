namespace GcLib;

public sealed partial class VirtualCam
{
    /// <inheritdoc/>
    public override uint PayloadSize => (uint)(_genApi.Height * _genApi.Width * _genApi.PixelSize / 8);

    /// <inheritdoc/>
    public override uint BufferCapacity { get; set; }

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