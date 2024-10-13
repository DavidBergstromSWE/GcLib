using System;
using System.Globalization;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts integer timestamp (number of ticks) to a string representation using format "HH:mm:ss.fff".
/// </summary>
[ValueConversion(typeof(ulong), typeof(string))]
internal sealed class TimestampConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return new DateTime(System.Convert.ToInt64((ulong)value)).ToString("HH:mm:ss.fff");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(TimestampConverter)} is a one-way converter.");
    }
}