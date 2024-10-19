using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImagerViewer.Utilities.Services;

namespace ImagerViewer.ViewModels;

/// <summary>
/// View model for handling UI options.
/// </summary>
internal sealed class OptionsWindowViewModel : ObservableObject
{
    #region Fields

    // backing-fields
    private IOptionsSubViewModel _currentOptionsViewModel;

    /// <summary>
    /// Stores the name of the sub-view that was last opened (before closing the Options window).
    /// </summary>
    private static string _lastOpenedOptionsViewModelName;

    #endregion

    #region Properties

    /// <summary>
    /// Available sub-views.
    /// </summary>
    public List<IOptionsSubViewModel> OptionsViewModels { get; }

    /// <summary>
    /// Currently selected sub-view.
    /// </summary>
    public IOptionsSubViewModel CurrentOptionsViewModel
    {
        get => _currentOptionsViewModel;
        set
        {
            if (SetProperty(ref _currentOptionsViewModel, value))
            {
                // Notify command of changes.
                ChangeOptionsViewCommand?.NotifyCanExecuteChanged();

                _lastOpenedOptionsViewModelName = _currentOptionsViewModel.Name;
            }
        }
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Instantiates a new view model for handling UI options.
    /// </summary>
    public OptionsWindowViewModel(IMetroWindowService windowService, MainWindowViewModel mainViewModel, DeviceViewModel deviceViewModel, ImageProcessingViewModel imageProcessingViewModel, ImageDisplayViewModel imageDisplayViewModel, HistogramViewModel histogramViewModel, AcquisitionViewModel acquisitionViewModel)
    {
        // Instantiate sub-views and add to list of available ones.
        OptionsViewModels =
        [
            new OptionsDeviceViewModel(deviceViewModel),
            new OptionsImageProcessingViewModel(imageProcessingViewModel),
            new OptionsDisplayViewModel(imageDisplayViewModel),
            new OptionsHistogramViewModel(histogramViewModel),
            new OptionsAcquisitionViewModel(windowService, acquisitionViewModel),
            new OptionsInterfaceViewModel(mainViewModel),
        ];

        // Set default selected sub-view.
        CurrentOptionsViewModel = _lastOpenedOptionsViewModelName != null
            ? OptionsViewModels.Find(vm => vm.Name == _lastOpenedOptionsViewModelName)
            : OptionsViewModels[0];

        // Instantiate commands.
        ChangeOptionsViewCommand = new RelayCommand<IOptionsSubViewModel>(ChangeOptionsView, svm => svm is not null);
        CancelChangesCommand = new RelayCommand(CancelSettings);
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a request invoked by a UI command to change the selected sub-view.
    /// </summary>
    public IRelayCommand<IOptionsSubViewModel> ChangeOptionsViewCommand { get; }

    /// <summary>
    /// Relays a request invoked by a UI command to revert options to their previous settings.
    /// </summary>
    public IRelayCommand CancelChangesCommand { get; }

    #endregion

    #region Private methods

    /// <summary>
    /// Changes currently selected sub-view.
    /// </summary>
    /// <param name="viewModel">Viewmodel of sub-view to be selected.</param>
    private void ChangeOptionsView(IOptionsSubViewModel viewModel)
    {
        if (OptionsViewModels.Contains(viewModel))
            CurrentOptionsViewModel = OptionsViewModels.FirstOrDefault(vm => vm == viewModel);
    }

    /// <summary>
    /// Revert settings in all sub-views to their previous values.
    /// </summary>
    private void CancelSettings()
    {
        foreach (IOptionsSubViewModel vm in OptionsViewModels)
            vm.CancelChanges();
    }

    #endregion
}