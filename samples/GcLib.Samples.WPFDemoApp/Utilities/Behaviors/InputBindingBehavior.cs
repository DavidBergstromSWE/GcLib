using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace FusionViewer.Utilities.Behaviors;

/// <summary>
/// Attached behaviour allowing inputbindings to be propagated from a UserControl or FrameworkElement to a parent window. 
/// </summary>
internal sealed class InputBindingBehavior
{
    public static readonly DependencyProperty PropagateInputBindingsToWindowProperty =
        DependencyProperty.RegisterAttached("PropagateInputBindingsToWindow", typeof(bool), typeof(InputBindingBehavior),
            new PropertyMetadata(false, OnPropagateInputBindingsToWindowChanged));

    private static readonly Dictionary<int, Tuple<WeakReference<FrameworkElement>, List<InputBinding>>> trackedFrameWorkElementsToBindings =
        [];

    public static bool GetPropagateInputBindingsToWindow(FrameworkElement obj)
    {
        return (bool)obj.GetValue(PropagateInputBindingsToWindowProperty);
    }

    public static void SetPropagateInputBindingsToWindow(FrameworkElement obj, bool value)
    {
        obj.SetValue(PropagateInputBindingsToWindowProperty, value);
    }

    private static void OnPropagateInputBindingsToWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((FrameworkElement)d).Loaded += OnFrameworkElementLoaded;
        ((FrameworkElement)d).Unloaded += OnFrameworkElementUnLoaded;
    }

    private static void OnFrameworkElementLoaded(object sender, RoutedEventArgs e)
    {
        var frameworkElement = (FrameworkElement)sender;

        var window = Window.GetWindow(frameworkElement);
        if (window != null)
        {
            // transfer InputBindings into our control
            if (!trackedFrameWorkElementsToBindings.TryGetValue(frameworkElement.GetHashCode(), out Tuple<WeakReference<FrameworkElement>, List<InputBinding>> trackingData))
            {
                trackingData = Tuple.Create(
                    new WeakReference<FrameworkElement>(frameworkElement),
                    frameworkElement.InputBindings.Cast<InputBinding>().ToList());

                trackedFrameWorkElementsToBindings.Add(
                    frameworkElement.GetHashCode(), trackingData);
            }

            // apply Bindings to Window
            foreach (InputBinding inputBinding in trackingData.Item2)
            {
                _ = window.InputBindings.Add(inputBinding); // throws "Cannot find governing FrameworkElement or FrameworkContentElement for target element."?
            }

            frameworkElement.InputBindings.Clear();
        }
    }

    private static void OnFrameworkElementUnLoaded(object sender, RoutedEventArgs e)
    {
        var frameworkElement = (FrameworkElement)sender;
        var window = Window.GetWindow(frameworkElement);
        int hashCode = frameworkElement.GetHashCode();

        // remove Bindings from Window
        if (window != null)
        {
            if (trackedFrameWorkElementsToBindings.TryGetValue(hashCode, out Tuple<WeakReference<FrameworkElement>, List<InputBinding>> trackedData))
            {
                foreach (InputBinding binding in trackedData.Item2)
                {
                    _ = frameworkElement.InputBindings.Add(binding);
                    window.InputBindings.Remove(binding);
                }
                trackedData.Item2.Clear();
                _ = trackedFrameWorkElementsToBindings.Remove(hashCode);

                // catch removed and orphaned entries
                CleanupBindingsDictionary(window, trackedFrameWorkElementsToBindings);
            }
        }
    }

    private static void CleanupBindingsDictionary(Window window, Dictionary<int, Tuple<WeakReference<FrameworkElement>, List<InputBinding>>> bindingsDictionary)
    {
        foreach (int hashCode in bindingsDictionary.Keys.ToList())
        {
            if (bindingsDictionary.TryGetValue(hashCode, out Tuple<WeakReference<FrameworkElement>, List<InputBinding>> trackedData) &&
                !trackedData.Item1.TryGetTarget(out _))
            {
                Debug.WriteLine($"InputBindingBehavior: FrameWorkElement {hashCode} did never unload but was GCed, cleaning up leftover KeyBindings");

                foreach (InputBinding binding in trackedData.Item2)
                {
                    window.InputBindings.Remove(binding);
                }

                trackedData.Item2.Clear();
                _ = bindingsDictionary.Remove(hashCode);
            }
        }
    }
}