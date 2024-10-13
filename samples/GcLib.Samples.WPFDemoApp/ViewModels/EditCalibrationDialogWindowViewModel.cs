using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FusionViewer.Models;

namespace FusionViewer.ViewModels;

/// <summary>
/// Models a dialog window for editing existing geometric calibration data.
/// </summary>
internal class EditCalibrationDialogWindowViewModel : ObservableValidator
{
    #region Fields

    // backing-fields
    private double _x00;
    private double _x01;
    private double _x02;
    private double _x10;
    private double _x11;
    private double _x12;

    /// <summary>
    /// Image model containing calibration data.
    /// </summary>
    private readonly FusedImageModel _fusedImageModel;

    #endregion

    #region Properties

    /// <summary>
    /// Matrix element.
    /// </summary>
    [Required]
    public double X00
    {
        get => _x00;
        set
        {
            if (SetProperty(ref _x00, value, validate: true))
                _fusedImageModel.CalibrationData.AffineTransform[0,0] = _x00;
        }
    }

    /// <summary>
    /// Matrix element.
    /// </summary>
    [Required]
    public double X01
    {
        get => _x01;
        set
        {
            if (SetProperty(ref _x01, value, true))
                _fusedImageModel.CalibrationData.AffineTransform[0, 1] = _x01;
        }
    }

    /// <summary>
    /// Matrix element.
    /// </summary>
    [Required]
    public double X02
    {
        get => _x02;
        set 
        {
            if (SetProperty(ref _x02, value, true))
                _fusedImageModel.CalibrationData.AffineTransform[0, 2] = _x02;
        }
    }

    /// <summary>
    /// Matrix element.
    /// </summary>
    [Required]
    public double X10
    {
        get => _x10;
        set 
        {
            if (SetProperty(ref _x10, value, true))
                _fusedImageModel.CalibrationData.AffineTransform[1, 0] = _x10;
        }
    }

    /// <summary>
    /// Matrix element.
    /// </summary>
    [Required]
    public double X11
    {
        get => _x11;
        set 
        {
            if (SetProperty(ref _x11, value, true))
                _fusedImageModel.CalibrationData.AffineTransform[1, 1] = _x11;
        }
    }

    /// <summary>
    /// Matrix element.
    /// </summary>
    [Required]
    public double X12
    {
        get => _x12;
        set 
        {
            if (SetProperty(ref _x12, value, true))
                _fusedImageModel.CalibrationData.AffineTransform[1, 2] = _x12;
        }
    }

    #endregion

    #region Commands

    /// <summary>
    /// Relays a UI request to auto-scale geometric calibration to match image sizes of input channels.
    /// </summary>
    public IRelayCommand AutoScaleCalibrationDataCommand { get; }

    /// <summary>
    /// Relays a UI request to clear an existing geometric calibration.
    /// </summary>
    public IRelayCommand ClearCalibrationDataCommand { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Create a new model for a dialog window supporting editing of existing geometric calibration data.
    /// </summary>
    /// <param name="imageModel">Image model containing calibration data.</param>
    public EditCalibrationDialogWindowViewModel(FusedImageModel imageModel)
    {
        _fusedImageModel = imageModel;

        // Setup matrix elements.
        _x00 = _fusedImageModel.CalibrationData.AffineTransform[0, 0];
        _x01 = _fusedImageModel.CalibrationData.AffineTransform[0, 1];
        _x02 = _fusedImageModel.CalibrationData.AffineTransform[0, 2];
        _x10 = _fusedImageModel.CalibrationData.AffineTransform[1, 0];
        _x11 = _fusedImageModel.CalibrationData.AffineTransform[1, 1];
        _x12 = _fusedImageModel.CalibrationData.AffineTransform[1, 2];

        AutoScaleCalibrationDataCommand = new RelayCommand(AutoScaleCalibrationData);
        ClearCalibrationDataCommand = new RelayCommand(ClearCalibrationData);
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Auto-scales calibration data to match image sizes of input channels.
    /// </summary>
    private void AutoScaleCalibrationData()
    {
        if (_fusedImageModel.OutputImageSize.IsEmpty)
            return;

        // Re-scale.
        X00 = _fusedImageModel.Channel2Image.Width / (double)_fusedImageModel.Channel1Image.Width;
        X11 = _fusedImageModel.Channel2Image.Height / (double)_fusedImageModel.Channel1Image.Height;
    }

    /// <summary>
    /// Clear geometric calibration data.
    /// </summary>
    private void ClearCalibrationData()
    {
        X00 = 1; X01 = 0; X02 = 0;
        X10 = 0; X11 = 1; X12 = 0;
    }

    #endregion
}