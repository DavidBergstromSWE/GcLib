using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace FusionViewer.Converters;

/// <summary>
/// Converts <see cref="Stretch"/> value to <see cref="ScrollBarVisibility"/> value. Converted value will be Auto if no stretch is used and Disabled otherwise.
/// </summary>
[ValueConversion(typeof(Stretch), typeof(ScrollBarVisibility))]
internal sealed class StretchToScrollBarVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (Stretch)value == Stretch.None ? ScrollBarVisibility.Auto : (object)ScrollBarVisibility.Disabled;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(StretchToScrollBarVisibilityConverter)} is a one-way converter.");
    }
}