using System;
using System.Globalization;
using System.Windows.Data;

namespace ImagerViewer.Converters;

/// <summary>
/// Converts a boolean value to its inverse value (e.g. <see langword="true"/> to <see langword="false"/> and <see langword="false"/> to <see langword="true"/>).
/// </summary>
[ValueConversion(sourceType: typeof(bool), targetType: typeof(bool))]
internal sealed class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return !(bool)value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return Convert(value, targetType, parameter, culture);
    }
}