namespace GcLib;

/// <summary>
/// Image compression modes.
/// </summary>
public enum ImageCompressionMode
{
    /// <summary>
    /// Default value. Image compression is disabled. Images are transmitted uncompressed.
    /// </summary>
    Off,
    /// <summary>
    /// JPEG compression is selected.
    /// </summary>
    JPEG,
    /// <summary>
    /// JPEG 2000 compression is selected.
    /// </summary>
    JPEG2000,
    /// <summary>
    /// H.264 compression is selected.
    /// </summary>
    H264,
    /// <summary>
    /// H.265 compression is selected.
    /// </summary>
    H265
}
