namespace FusionViewer;

/// <summary>
/// Stores data for an image histogram.
/// </summary>
/// <remarks>
/// Creates a new image histogram.
/// </remarks>
/// <param name="data">Histogram data.</param>
/// <param name="maximum">Maximum intensity value.</param>
/// <param name="numChannels">Number of image channels.</param>
public class ImageHistogram(double[,] data, uint maximum, uint numChannels)
{
    /// <summary>
    /// Histogram data, stored as a two-dimensional array with image channel as first dimension (BGR) and histogram data as second dimension.
    /// </summary>
    public double[,] Data { get; init; } = data;

    /// <summary>
    /// Maximum intensity value of histogram data.
    /// </summary>
    public uint Maximum { get; init; } = maximum;

    /// <summary>
    /// Number of image channels in histogram data.
    /// </summary>
    public uint NumChannels { get; init; } = numChannels;

    public bool ContainsData => Data != null;
}