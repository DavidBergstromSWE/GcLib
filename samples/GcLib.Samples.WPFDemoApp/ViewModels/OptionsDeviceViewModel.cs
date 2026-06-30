using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for handling device-related options.
/// </summary>
internal sealed class OptionsDeviceViewModel : ObservableObject, IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly Visibility _initialVisibility;
    private readonly uint _initialDeviceParameterUpdateTimeDelay;

    #endregion

    #region Properties

    /// <summary>
    /// Reference to parent view model.
    /// </summary>
    public DeviceViewModel DeviceViewModel { get; init; }

    /// <inheritdoc/>
    public string Name => "Devices";

    #endregion

    #region Commands

    /// <summary>
    /// Relays a request to change parameter visibility.
    /// </summary>
    public IRelayCommand<Visibility> ChangeParameterVisibilityCommand { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new view model for handling device-related options.
    /// </summary>
    public OptionsDeviceViewModel(DeviceViewModel deviceViewModel)
    {
        DeviceViewModel = deviceViewModel;

        // Store initial settings.
        _initialVisibility = DeviceViewModel.UserVisibility;
        _initialDeviceParameterUpdateTimeDelay = DeviceViewModel.DeviceParameterUpdateTimeDelay;

        // Instantiate members.
        ChangeParameterVisibilityCommand = new RelayCommand<Visibility>(p => DeviceViewModel.UserVisibility = p);
    }

    #endregion

    #region Public methods

    /// <inheritdoc/>
    public void CancelChanges()
    {
        // Restore initial settings.
        DeviceViewModel.UserVisibility = _initialVisibility;
        DeviceViewModel.DeviceParameterUpdateTimeDelay = _initialDeviceParameterUpdateTimeDelay;
    }

    #endregion
}