using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FusionViewer.Utilities.Services;

namespace FusionViewer.ViewModels;

/// <summary>
/// View model for handling recording-related options.
/// </summary>
internal sealed class OptionsAcquisitionViewModel : ObservableObject, IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly bool _initialSaveChannel1Data;
    private readonly string _initialChannel1FilePath;
    private readonly bool _initialAutoGenerateFileNames;
    private readonly bool _initialLogTimeStamps;
    private readonly bool _initialAppendTimeStamps;

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
    /// Relays a request invoked by a UI command to open a dialogue window for letting user select a new file path for recorded channel 1 data.
    /// </summary>
    public IRelayCommand<string> BrowseChannel1FilePathCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to open a dialogue window for letting user select a new file path for recorded channel 2 data.
    /// </summary>
    public IRelayCommand<string> BrowseChannel2FilePathCommand { get; }

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
        _initialSaveChannel1Data = AcquisitionViewModel.AcquisitionChannel.SaveRawData;
        _initialChannel1FilePath = AcquisitionViewModel.AcquisitionChannel.FilePath;
        _initialAutoGenerateFileNames = AcquisitionViewModel.AutoGenerateFileNames;
        _initialLogTimeStamps = AcquisitionViewModel.LogTimeStamps;
        _initialAppendTimeStamps = AcquisitionViewModel.AppendTimeStamps;

        // Instantiate commands.
        BrowseChannel1FilePathCommand = new RelayCommand<string>(s => AcquisitionViewModel.AcquisitionChannel.FilePath = FindFilePath(s));
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        AcquisitionViewModel.AcquisitionChannel.SaveRawData = _initialSaveChannel1Data;
        AcquisitionViewModel.AcquisitionChannel.FilePath = _initialChannel1FilePath;
        AcquisitionViewModel.AutoGenerateFileNames = _initialAutoGenerateFileNames;
        AcquisitionViewModel.LogTimeStamps = _initialLogTimeStamps;
        AcquisitionViewModel.AppendTimeStamps = _initialAppendTimeStamps;
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