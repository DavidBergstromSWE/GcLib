using System.IO;
using CommunityToolkit.Mvvm.Input;
using ImagerViewer.Utilities.Services;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for handling recording-related options.
/// </summary>
internal sealed class OptionsAcquisitionViewModel : IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly bool _initialSaveBinaryData;
    private readonly bool _initialSaveVideo;
    private readonly string _initialBinaryFilePath;
    private readonly string _initialVideoFilePath;
    private readonly bool _initialAutoGenerateFileNames;

    /// <summary>
    /// Service providing windows and dialogs.
    /// </summary>
    private readonly IMetroWindowService _windowService;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public string Name => "Recording";

    /// <summary>
    /// Reference to parent view model.
    /// </summary>
    public AcquisitionViewModel AcquisitionViewModel { get; init; }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a request invoked by a UI command to open a dialogue window for letting user select a new file path for recorded data.
    /// </summary>
    public IRelayCommand<string> BrowseBinaryFilePathCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to open a dialogue window for letting user select a new file path for video.
    /// </summary>
    public IRelayCommand<string> BrowseVideoFilePathCommand { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new view model for handling recording-related options.
    /// </summary>
    public OptionsAcquisitionViewModel(IMetroWindowService windowService, AcquisitionViewModel acquisitionViewModel)
    {
        AcquisitionViewModel = acquisitionViewModel;
        _windowService = windowService;

        // Store initial settings.
        _initialSaveBinaryData = AcquisitionViewModel.AcquisitionChannel.SaveRawData;
        _initialSaveVideo = AcquisitionViewModel.AcquisitionChannel.SaveVideo;
        _initialBinaryFilePath = AcquisitionViewModel.AcquisitionChannel.BinaryFilePath;
        _initialVideoFilePath = AcquisitionViewModel.AcquisitionChannel.VideoFilePath;
        _initialAutoGenerateFileNames = AcquisitionViewModel.AutoGenerateBinaryFileNames;

        // Instantiate commands.
        BrowseBinaryFilePathCommand = new RelayCommand<string>(s => AcquisitionViewModel.AcquisitionChannel.BinaryFilePath = FindFilePath(s, "bin"));
        BrowseVideoFilePathCommand = new RelayCommand<string>(s => AcquisitionViewModel.AcquisitionChannel.VideoFilePath = FindFilePath(s, "avi"));
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        AcquisitionViewModel.AcquisitionChannel.SaveRawData = _initialSaveBinaryData;
        AcquisitionViewModel.AcquisitionChannel.SaveVideo = _initialSaveVideo;
        AcquisitionViewModel.AcquisitionChannel.BinaryFilePath = _initialBinaryFilePath;
        AcquisitionViewModel.AcquisitionChannel.VideoFilePath = _initialVideoFilePath;
        AcquisitionViewModel.AutoGenerateBinaryFileNames = _initialAutoGenerateFileNames;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Opens a dialogue window for letting user select a file path.
    /// </summary>
    /// <param name="initialFilePath">Initial file path.</param>
    /// <returns>User-selected file path (or initial file path if cancelled).</returns>
    private string FindFilePath(string initialFilePath, string extension)
    {
        // Retrieve filepath from dialog.
        string filePath = _windowService.ShowSaveFileDialog(title: "Select output file", fileName: initialFilePath, filter: $"{extension} files (*.{extension})|*.{extension}", defaultExtension: extension, defaultPath: Path.GetDirectoryName(initialFilePath));
        return string.IsNullOrEmpty(filePath) ? initialFilePath : filePath;
    }

    #endregion
}