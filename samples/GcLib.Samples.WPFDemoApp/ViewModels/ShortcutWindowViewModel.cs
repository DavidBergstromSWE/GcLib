using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ImagerViewer.ViewModels;

/// <summary>
/// Models a view for displaying keyboard shortcuts (keybindings).
/// </summary>
internal sealed class ShortcutWindowViewModel : ObservableObject
{
    /// <summary>
    /// Available shortcuts/keybindings.
    /// </summary>
    public ObservableCollection<Shortcut> Shortcuts { get; private set; } = [];

    /// <summary>
    /// Creates a new viewmodel for displaying keyboard shortcuts.
    /// </summary>
    public ShortcutWindowViewModel()
    {
        // Load/save
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.L, ModifierKeys.Control), "I/O", "Load configuration"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.S, ModifierKeys.Control), "I/O", "Save configuration"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.R, ModifierKeys.Control), "I/O", "Open recorded sequence"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.X, ModifierKeys.Control), "I/O", "Close recorded sequence"));

        // Acquisition
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.F6), "Acquisition", "Start live view"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.F7), "Acquisition", "Stop live view or recording"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.F8), "Acquisition", "Start recording"));

        // Logging
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.F10, ModifierKeys.Shift), "Other", "Show log viewer"));

        // New windows
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.F11), "Display", "Show or exit full screen mode"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.F12), "Other", "Show keyboard shortcuts"));

        // Playback
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.Space), "Playback", "Play or pause (playback)"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.Right), "Playback", "Step one image forward (playback)"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.Left), "Playback", "Step one image back (playback)"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.Right, ModifierKeys.Shift), "Playback", "Step 10 images forward (playback)"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.Left, ModifierKeys.Shift), "Playback", "Step 10 images back (playback)"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.Right, ModifierKeys.Control), "Playback", "Go to end of sequence (playback)"));
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.Left, ModifierKeys.Control), "Playback", "Go to start of sequence (playback)"));

        // Themes
        Shortcuts.Add(new Shortcut(new KeyGesture(Key.T, ModifierKeys.Alt), "Other", "Toggle between light and dark theme"));
    }
}