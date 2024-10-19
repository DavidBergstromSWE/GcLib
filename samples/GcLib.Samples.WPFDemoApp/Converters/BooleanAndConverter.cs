using System;
using System.Windows.Data;

namespace ImagerViewer.Converters;

/// <summary>
/// Converts multiple boolean values to a single boolean value by a logical AND operation.
/// </summary>
internal sealed class BooleanAndConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    {
        foreach (object value in values)
        {
            if ((value is bool boolean) && boolean == false)
                return false;
        }

        return true;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(BooleanAndConverter)} is a one-way converter.");
    }
}