using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using GcLib;
using ImagerViewer.Utilities.Imaging;

namespace ImagerViewer.Converters;

/// <summary>
/// Converts an image of <see cref="GcBuffer"/> type to an image of <see cref="BitmapSource"/> type.
/// </summary>
[ValueConversion(typeof(GcBuffer), typeof(BitmapSource))]
internal sealed class GcBufferToBitmapSourceConverter : IValueConverter, IMultiValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GcBuffer buffer)
            return BitmapSourceExtensions.ToBitmapSource(buffer);

        return null;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is GcBuffer buffer && values[1] is bool isEnabled && isEnabled)
            return BitmapSourceExtensions.ToBitmapSource(buffer);

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}