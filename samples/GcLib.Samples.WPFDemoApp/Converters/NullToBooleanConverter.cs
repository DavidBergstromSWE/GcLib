using System;
using System.Globalization;
using System.Windows.Data;

namespace ImagerViewerApp.Converters;

/// <summary>
/// Converts a (nullable) <see cref="object"/> to a <see cref="bool"/> value. The converted value will be <see langword="false"/> if object is null and <see langword="true"/> if object is not null.
/// </summary>
[ValueConversion(typeof(object), typeof(bool))]
internal sealed class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(NullToBooleanConverter)} is a one-way converter.");
    }
}