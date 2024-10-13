using System;
using System.Globalization;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts a double value to its (natural) logarithmic value.
/// </summary>
[ValueConversion(sourceType: typeof(double), targetType: typeof(double))]
internal sealed class FloatLogScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value == 0 ? 0.0 : Math.Log((double)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value == 0 ? 0.0 : (double)Math.Exp((double)value);
    }
}