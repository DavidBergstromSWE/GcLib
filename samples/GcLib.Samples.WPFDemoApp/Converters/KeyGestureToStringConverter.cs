using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Input;

namespace ImagerViewerApp.Converters;

/// <summary>
/// Converts a <see cref="KeyGesture"/> to a display-friendly string.
/// </summary>
[ValueConversion(sourceType: typeof(KeyGesture), targetType: typeof(string))]
internal sealed class KeyGestureToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        KeyGesture keyGesture = (KeyGesture)value;

        string modifier = keyGesture.Modifiers switch
        {
            ModifierKeys.None => string.Empty,
            ModifierKeys.Alt => "Alt + ",
            ModifierKeys.Control => "Ctrl + ",
            ModifierKeys.Shift => "Shift + ",
            ModifierKeys.Windows => "Win + ",
            _ => string.Empty,
        };

        return modifier + keyGesture.Key.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
