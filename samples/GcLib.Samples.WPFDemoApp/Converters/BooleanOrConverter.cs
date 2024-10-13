using System;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts multiple boolean values to a single boolean value by a logical OR operation.
/// </summary>
internal sealed class BooleanOrConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        foreach (object value in values)
        {
            if ((value is bool boolean) && boolean == true)
                return true;
        }

        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(BooleanOrConverter)} is a one-way converter.");
    }
}