using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts two boolean values to a <see cref="System.Windows.Visibility"/> value by a logical AND operation. The converted value will be Visible if both booleans are true and Collapsed otherwise.
/// </summary>
internal sealed class BooleanAndToVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null)
            return System.Windows.Visibility.Visible;

        return values.Select(GetBool).All(b => b)
            ? System.Windows.Visibility.Visible
            : parameter is System.Windows.Visibility visibility ? visibility : System.Windows.Visibility.Hidden;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(BooleanAndToVisibilityConverter)} is a one-way converter.");
    }

    private static bool GetBool(object value)
    {
        return value is bool boolean && boolean;
    }
}