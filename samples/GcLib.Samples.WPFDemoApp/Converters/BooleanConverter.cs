using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Converts a <see cref="bool"/> value to configurable target values for <see langword="true"/> and <see langword="false"/>.
/// </summary>
/// <typeparam name="T">Target type.</typeparam>
/// <param name="trueValue">True value.</param>
/// <param name="falseValue">False value.</param>
public class BooleanConverter<T>(T trueValue, T falseValue) : IValueConverter
{
    /// <summary>
    /// True value.
    /// </summary>
    public T True { get; set; } = trueValue;

    /// <summary>
    /// False value.
    /// </summary>
    public T False { get; set; } = falseValue;

    public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool boolean && boolean ? True : False;
    }

    public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is T t && EqualityComparer<T>.Default.Equals(t, True);
    }
}