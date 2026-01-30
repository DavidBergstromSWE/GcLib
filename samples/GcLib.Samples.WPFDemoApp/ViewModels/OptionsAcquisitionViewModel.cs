using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
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
    private readonly bool _initialSaveData;
    private readonly string _initialFilePath;
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
    public IRelayCommand<string> BrowseChannelFilePathCommand { get; }

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
        _initialSaveData = AcquisitionViewModel.AcquisitionChannel.SaveRawData;
        _initialFilePath = AcquisitionViewModel.AcquisitionChannel.FilePath;
        _initialAutoGenerateFileNames = AcquisitionViewModel.AutoGenerateFileNames;

        // Instantiate commands.
        BrowseChannelFilePathCommand = new RelayCommand<string>(s => AcquisitionViewModel.AcquisitionChannel.FilePath = FindFilePath(s));
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        AcquisitionViewModel.AcquisitionChannel.SaveRawData = _initialSaveData;
        AcquisitionViewModel.AcquisitionChannel.FilePath = _initialFilePath;
        AcquisitionViewModel.AutoGenerateFileNames = _initialAutoGenerateFileNames;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Opens a dialogue window for letting user select a file path.
    /// </summary>
    /// <param name="initialFilePath">Initial file path.</param>
    /// <returns>User-selected file path (or initial file path if cancelled).</returns>
    private string FindFilePath(string initialFilePath)
    {
        // Retrieve filepath from dialog.
        string filePath = _windowService.ShowSaveFileDialog(title: "Select output file", fileName: initialFilePath, filter: "bin files (*.bin)|*.bin", defaultExtension: "bin", defaultPath: Path.GetDirectoryName(initialFilePath));
        return string.IsNullOrEmpty(filePath) ? initialFilePath : filePath;
    }

    #endregion
}