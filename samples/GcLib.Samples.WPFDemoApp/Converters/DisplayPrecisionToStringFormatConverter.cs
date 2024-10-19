using System;
using System.Globalization;
using System.Windows.Data;

namespace ImagerViewer.Converters;

/// <summary>
/// Converts display precision for a float number to a suitable string format representation.
/// </summary>
[ValueConversion(typeof(long), typeof(string))]
internal sealed class DisplayPrecisionToStringFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return "F" + System.Convert.ToString((long)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(DisplayPrecisionToStringFormatConverter)} is a one-way converter.");
    }
}