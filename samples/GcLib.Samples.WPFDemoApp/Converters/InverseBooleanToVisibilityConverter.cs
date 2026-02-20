namespace ImagerViewer.Converters;

public class InverseBooleanToVisibilityConverter : BooleanConverter<System.Windows.Visibility>
{
    public InverseBooleanToVisibilityConverter() : base(System.Windows.Visibility.Hidden, System.Windows.Visibility.Visible) { }
}