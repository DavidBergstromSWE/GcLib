using Microsoft.Xaml.Behaviors;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows;

namespace ImagerViewerApp.Utilities.Behaviors;

/// <summary>
/// Attachable behavior for objects of type <see cref="MenuItem"/> in a <see cref="ContextMenu"/>, where checkable menu items can be grouped together (similar to a group of radiobuttons).
/// </summary>
public class MenuItemButtonGroupBehavior : Behavior<MenuItem>
{
    public static readonly DependencyProperty TagProperty = DependencyProperty.Register(nameof(Tag), typeof(string), typeof(MenuItemButtonGroupBehavior), new PropertyMetadata(string.Empty));
    
    public string Tag
    {
        get { return (string)GetValue(TagProperty); }
        set { SetValue(TagProperty, value); }
    }

    protected override void OnAttached()    
    {
        base.OnAttached();

        GetCheckableSubMenuItems(AssociatedObject)
            .ToList()
            .ForEach(item => item.Click += OnClick);
    }

    protected override void OnDetaching()
    {
        base.OnDetaching();

        GetCheckableSubMenuItems(AssociatedObject)
            .ToList()
            .ForEach(item => item.Click -= OnClick);
    }

    private IEnumerable<MenuItem> GetCheckableSubMenuItems(ItemsControl menuItem)
    {
        var itemCollection = menuItem.Items;
        
        if (string.IsNullOrEmpty(Tag))
            return itemCollection.OfType<MenuItem>().Where(menuItemCandidate => menuItemCandidate.IsCheckable);
        
        return itemCollection.OfType<MenuItem>().Where(menuItemCandidate => menuItemCandidate.IsCheckable && (menuItemCandidate.Tag is string tag) && tag == Tag);
    }

    private void OnClick(object sender, RoutedEventArgs routedEventArgs)
    {
        var menuItem = (MenuItem)sender;

        if (menuItem.IsChecked == false)
        {
            menuItem.IsChecked = true;
            return;
        }

        GetCheckableSubMenuItems(AssociatedObject)
            .Where(item => item != menuItem)
            .ToList()
            .ForEach(item => item.IsChecked = false);
    }
}