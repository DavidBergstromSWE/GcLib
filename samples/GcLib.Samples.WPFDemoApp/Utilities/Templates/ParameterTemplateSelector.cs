using System.Windows;
using System.Windows.Controls;
using GcLib;

namespace FusionViewer.Utilities.Templates;

/// <summary>
/// Provides a way to choose a <see cref="DataTemplate"/> suited for a specific <see cref="GcParameter"/> type.
/// </summary>
internal sealed class ParameterTemplateSelector : DataTemplateSelector
{
    /// <summary>
    /// Selects a <see cref="DataTemplate"/> for a specific <see cref="GcParameter"/> type.
    /// </summary>
    /// <param name="item">The <see cref="GcParameter"/> object for which to select the template.</param>
    /// <param name="container">The data-bound object.</param>
    /// <returns><see cref="DataTemplate"/> or null.</returns>
    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {
        if (container is FrameworkElement element && item != null && item is GcParameter)
        {
            var parameter = item as GcParameter;

            switch (parameter.Type)
            {
                case GcParameterType.String:
                    return element.FindResource("StringTemplate") as DataTemplate;

                case GcParameterType.Integer:
                    var gcInteger = item as GcInteger;
                    return gcInteger.IncrementMode == EIncMode.listIncrement
                        ? element.FindResource("IntegerComboBoxTemplate") as DataTemplate
                        : gcInteger.IsWritable
                            ? (gcInteger.Max - gcInteger.Min) > 10000
                                                        ? element.FindResource("IntegerLogarithmicSliderTemplate") as DataTemplate
                                                        : element.FindResource("IntegerSliderTemplate") as DataTemplate
                            : element.FindResource("IntegerTextBlockTemplate") as DataTemplate;

                case GcParameterType.Float:
                    var gcFloat = item as GcFloat;
                    return gcFloat.IsWritable
                        ? gcFloat.Max - gcFloat.Min > 1000
                            ? element.FindResource("FloatLogarithmicSliderTemplate") as DataTemplate
                            : gcFloat.Increment > 0
                                ? element.FindResource("FloatSliderIncrementTemplate") as DataTemplate
                                : element.FindResource("FloatSliderTemplate") as DataTemplate
                        : element.FindResource("FloatTextBlockTemplate") as DataTemplate;

                case GcParameterType.Boolean:
                    return element.FindResource("BooleanTemplate") as DataTemplate;

                case GcParameterType.Enumeration:
                    return element.FindResource("EnumerationTemplate") as DataTemplate;

                case GcParameterType.Command:
                    return element.FindResource("CommandTemplate") as DataTemplate;
                default:
                    break;
            }
        }

        return base.SelectTemplate(item, container);
    }
}