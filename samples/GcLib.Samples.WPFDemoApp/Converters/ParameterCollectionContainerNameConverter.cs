using System;
using System.Globalization;
using System.Windows.Data;
using GcLib.Utilities.Collections;

namespace FusionViewer.Converters;

/// <summary>
/// Converts the container name of a <see cref="ReadOnlyParameterCollection"/> using the supplied parameter.
/// </summary>
internal class ParameterCollectionContainerNameConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new FormatException();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}