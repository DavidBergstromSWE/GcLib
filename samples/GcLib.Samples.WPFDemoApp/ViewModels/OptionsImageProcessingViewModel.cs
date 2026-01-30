using CommunityToolkit.Mvvm.ComponentModel;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for handling image processing related options.
/// </summary>
internal sealed class OptionsImageProcessingViewModel : IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly bool _flipChannel1Horizontal;
    private readonly bool _flipChannel1Vertical;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public string Name => "ImageProcessing";

    /// <summary>
    /// Reference to parent view model.
    /// </summary>
    public ImageProcessingViewModel ImageProcessingViewModel { get; init; }

    #endregion

    #region Constructor

    /// <summary>
    /// Instantiates a new view model for handling image processing related options.
    /// </summary>
    public OptionsImageProcessingViewModel(ImageProcessingViewModel imageProcessingViewModel)
    {
        ImageProcessingViewModel = imageProcessingViewModel;

        // Store initial settings.
        _flipChannel1Horizontal = ImageProcessingViewModel.ImageChannel.FlipHorizontal;
        _flipChannel1Vertical = ImageProcessingViewModel.ImageChannel.FlipVertical;
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        ImageProcessingViewModel.ImageChannel.FlipHorizontal = _flipChannel1Horizontal;
        ImageProcessingViewModel.ImageChannel.FlipVertical = _flipChannel1Vertical;
    }

    #endregion
}