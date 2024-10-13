using System;
using System.Globalization;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts a (nullable) <see cref="object"/> to a <see cref="System.Windows.Visibility"/> value.
/// If object is null, the converted value will be set to provided <paramref name="parameter"/>. If no parameter is provided it will be set to <see cref="System.Windows.Visibility.Hidden"/>.
/// If object is not null, the converted value will be set to <see cref="System.Windows.Visibility.Visible"/>.
/// </summary>
[ValueConversion(typeof(object), typeof(System.Windows.Visibility))]
internal sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is System.Windows.Visibility visibility)
            return value == null ? visibility : System.Windows.Visibility.Visible;
        else return value == null ? System.Windows.Visibility.Hidden : System.Windows.Visibility.Visible;

    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(NullToVisibilityConverter)} is a one-way converter.");
    }
}