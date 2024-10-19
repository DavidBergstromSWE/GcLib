using GcLib;

namespace ImagerViewer.Utilities.Messages;

/// <summary>
/// A message announcing the updating of images.
/// </summary>
/// <param name="sourceImage">Updated raw image.</param>
/// <param name="processedImage">Updated processed image.</param>
internal sealed class ImagesUpdatedMessage(GcBuffer sourceImage, GcBuffer processedImage)
{
    /// <summary>
    /// Updated raw image.
    /// </summary>
    public GcBuffer SourceImage { get; } = sourceImage;

    /// <summary>
    /// Updated processed image.
    /// </summary>
    public GcBuffer ProcessedImage { get; } = processedImage;
}
