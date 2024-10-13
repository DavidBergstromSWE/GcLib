using System;
using System.Globalization;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts an array of pixel values to a string representation.
/// </summary>
[ValueConversion(typeof(double[]), typeof(string))]
internal sealed class PixelValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double[] pixelValues = (double[])value;

        if (pixelValues == null)
            return null;

        // Update pixel value (format according to number of channels).
        return pixelValues.Length == 1 ? $"I: {pixelValues[0]}" : $"R: {pixelValues[2]}, G: {pixelValues[1]}, B: {pixelValues[0]}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(PixelValueConverter)} is a one-way converter.");
    }
}