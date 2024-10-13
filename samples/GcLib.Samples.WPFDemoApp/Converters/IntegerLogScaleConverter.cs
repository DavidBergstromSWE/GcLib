using System;
using System.Globalization;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts an integer value to its (natural) logarithmic value.
/// </summary>
[ValueConversion(sourceType: typeof(long), targetType: typeof(long))]
internal sealed class IntegerLogScaleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (long)value == 0 ? 0 : (int)Math.Log(System.Convert.ToDouble((long)value));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return System.Convert.ToInt64(value) == 0 ? 0 : (long)Math.Exp((double)value);
    }
}