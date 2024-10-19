using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ImagerViewerApp.ViewModels;

/// <summary>
/// View model for handling device-related options.
/// </summary>
internal sealed class OptionsDeviceViewModel : ObservableObject, IOptionsSubViewModel
{
    #region Fields

    // Initial settings.
    private readonly Visibility _initialVisibility;
    private readonly uint _initialDeviceParameterUpdateTimeDelay;

    // backing-fields
    private uint _deviceParameterUpdateTimeDelay;

    #endregion

    #region Properties

    /// <summary>
    /// Reference to parent view model.
    /// </summary>
    public DeviceViewModel DeviceViewModel { get; init; }

    /// <inheritdoc/>
    public string Name => "Devices";

    /// <summary>
    /// Selected user visibility level for device interaction.
    /// </summary>
    public Visibility SelectedVisibility => DeviceViewModel.UserVisibility;

    /// <summary>
    /// Time delay before updating device parameter after changing value (in milliseconds).
    /// </summary>
    public uint DeviceParameterUpdateTimeDelay
    {
        get => _deviceParameterUpdateTimeDelay;
        set
        {
            if (SetProperty(field: ref _deviceParameterUpdateTimeDelay, newValue: value))
            {
                DeviceViewModel.DeviceParameterUpdateTimeDelay = value;
            }
        }
    }

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
        DeviceParameterUpdateTimeDelay = _initialDeviceParameterUpdateTimeDelay;
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