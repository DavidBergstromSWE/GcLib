using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;

namespace FusionViewer.ViewModels;

/// <summary>
/// View model for handling image processing related options.
/// </summary>
internal sealed class OptionsImageProcessingViewModel : ObservableObject, IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly BorderType _selectedBorderType;
    private readonly Inter _selectedInterpolationMethod;
    private readonly bool _flipChannel1Horizontal;
    private readonly bool _flipChannel1Vertical;
    private readonly bool _flipChannel2Horizontal;
    private readonly bool _flipChannel2Vertical;
    private readonly ScalingDirection _imageScalingMode;

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
        _selectedBorderType = ImageProcessingViewModel.FusedImageChannel.SelectedBorderType;
        _selectedInterpolationMethod = ImageProcessingViewModel.FusedImageChannel.SelectedInterpolationMethod;
        _flipChannel1Horizontal = ImageProcessingViewModel.ImageChannel1.FlipHorizontal;
        _flipChannel1Vertical = ImageProcessingViewModel.ImageChannel1.FlipVertical;
        _flipChannel2Horizontal = ImageProcessingViewModel.ImageChannel2.FlipHorizontal;
        _flipChannel2Vertical = ImageProcessingViewModel.ImageChannel2.FlipVertical;
        _imageScalingMode = ImageProcessingViewModel.FusedImageChannel.SelectedImageScalingDirection;
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        ImageProcessingViewModel.FusedImageChannel.SelectedBorderType = _selectedBorderType;
        ImageProcessingViewModel.FusedImageChannel.SelectedInterpolationMethod = _selectedInterpolationMethod;
        ImageProcessingViewModel.ImageChannel1.FlipHorizontal = _flipChannel1Horizontal;
        ImageProcessingViewModel.ImageChannel1.FlipVertical = _flipChannel1Vertical;
        ImageProcessingViewModel.ImageChannel2.FlipHorizontal = _flipChannel2Horizontal;
        ImageProcessingViewModel.ImageChannel2.FlipVertical = _flipChannel2Vertical;
        ImageProcessingViewModel.FusedImageChannel.SelectedImageScalingDirection = _imageScalingMode;
    }

    #endregion
}