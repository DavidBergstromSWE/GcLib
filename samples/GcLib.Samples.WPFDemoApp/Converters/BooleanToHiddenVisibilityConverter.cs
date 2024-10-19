using System;
using System.Globalization;
using System.Windows.Data;

namespace ImagerViewerApp.Converters;

/// <summary>
/// Converts a boolean value to a <see cref="System.Windows.Visibility"/> value. The converted value will be Visible if boolean is true and Hidden otherwise.
/// </summary>
[ValueConversion(sourceType: typeof(bool), targetType: typeof(System.Windows.Visibility))]
internal sealed class BooleanToHiddenVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool flag = false;
        if (value is bool boolean)
        {
            flag = boolean;
        }
        else if (value is bool?)
        {
            bool? nullable = (bool?)value;
            flag = nullable ?? false;
        }
        return flag ? System.Windows.Visibility.Visible : parameter is System.Windows.Visibility visibility ? visibility : System.Windows.Visibility.Hidden;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(BooleanToHiddenVisibilityConverter)} is a one-way converter.");
    }
}