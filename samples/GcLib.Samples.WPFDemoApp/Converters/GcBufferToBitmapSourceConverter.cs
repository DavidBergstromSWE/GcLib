using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ImagerViewerApp.Utilities.Imaging;
using GcLib;

namespace ImagerViewerApp.Converters;

/// <summary>
/// Converts an image of <see cref="GcBuffer"/> type to an image of <see cref="BitmapSource"/> type.
/// </summary>
[ValueConversion(typeof(GcBuffer), typeof(BitmapSource))]
internal sealed class GcBufferToBitmapSourceConverter : IValueConverter, IMultiValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GcBuffer buffer)
            return GcBufferExtensions.ToBitmapSource(buffer);
        
        return null;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is GcBuffer buffer && values[1] is bool isEnabled && isEnabled)
            return GcBufferExtensions.ToBitmapSource(buffer);

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