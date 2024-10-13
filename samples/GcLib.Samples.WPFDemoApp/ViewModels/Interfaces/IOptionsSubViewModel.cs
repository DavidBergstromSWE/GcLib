namespace FusionViewer.ViewModels;

/// <summary>
/// Interface for a view model representing an options sub-view.
/// </summary>
public interface IOptionsSubViewModel
{
    /// <summary>
    /// Name of options sub-view.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Revert changes to settings in options sub-view to their previous values.
    /// </summary>
    public void CancelChanges();
}