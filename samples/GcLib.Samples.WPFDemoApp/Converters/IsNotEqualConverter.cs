﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace FusionViewer.Converters;

/// <summary>
/// Compares two objects and returns true if they are not equal and false if they are.
/// The converter supports both <see cref="MultiBinding"/> and single <see cref="Binding"/>.
/// In the latter case the second object is supplied as a converter parameter.
/// </summary>
[ValueConversion(sourceType: typeof(object), targetType: typeof(bool))]
internal sealed class IsNotEqualConverter : IValueConverter, IMultiValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.Equals(parameter) == false;
    }

    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        return values[0].Equals(values[1]) == false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(IsEqualConverter)} is a one-way converter.");
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException($"{nameof(IsEqualConverter)} is a one-way converter.");
    }
}