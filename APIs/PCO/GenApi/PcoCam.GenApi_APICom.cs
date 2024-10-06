using System;
using PCO.SDK;

namespace GcLib;

public partial class PcoCam
{
    private sealed partial class GenApi
    {
        /// <summary>
        /// Responds to changes of a GenApi parameter, relaying them to device and updating dependent parameters as necessary.
        /// </summary>
        /// <param name="eventArgs">Event arguments containing name of parameter that was changed.</param>
        public void OnParameterChanged(object sender, ParameterInvalidateEventArgs eventArgs)
        {
            string parameterName = eventArgs.ParameterName;

            // Update dependencies.
            if (parameterName == nameof(DeviceTemperatureSelector))
            {
                DeviceTemperature.Value = LibWrapper.GetCameraTemperature(_cameraHandle, (TemperatureSelector)_deviceTemperatureSelector.IntValue);
            }

            if (parameterName == nameof(Width))
            {
                OffsetX.ImposeMax(WidthMax - Width);

                UpdateROI();

                // Retain frame rate.
                OnParameterChanged(this, new ParameterInvalidateEventArgs(nameof(AcquisitionFrameRate)));
            }

            if (parameterName == nameof(Height))
            {
                OffsetY.ImposeMax(HeightMax - Height);

                UpdateROI();

                // Retain frame rate.
                OnParameterChanged(this, new ParameterInvalidateEventArgs(nameof(AcquisitionFrameRate)));
            }

            if (parameterName == nameof(OffsetX))
            {
                UpdateROI();
            }

            if (parameterName == nameof(OffsetY))
            {
                UpdateROI();
            }

            if (parameterName == nameof(BinningHorizontal))
            {
                // Set binning in camera.
                LibWrapper.SetBinning(_cameraHandle, (ushort)BinningHorizontal, BinningOrientation.Horizontal);

                var previousBinning = SensorWidth / WidthMax;
                var previousWidth = Width;
                var previousOffsetX = OffsetX;

                WidthMax.Value = SensorWidth / BinningHorizontal;
                if (previousBinning / BinningHorizontal < 1) // binning down
                {
                    Width.Value = previousWidth * previousBinning / BinningHorizontal;
                    Width.ImposeMax(WidthMax);
                    OffsetX.Value = previousOffsetX * previousBinning / BinningHorizontal;
                    OffsetX.ImposeMax(WidthMax - Width);
                }
                else // binning up
                {
                    Width.ImposeMax(WidthMax);
                    Width.Value = previousWidth * previousBinning / BinningHorizontal;
                    OffsetX.ImposeMax(WidthMax - Width);
                    OffsetX.Value = previousOffsetX * previousBinning / BinningHorizontal;
                }

                UpdateROI();

                // Retain frame rate.
                OnParameterChanged(this, new ParameterInvalidateEventArgs(nameof(AcquisitionFrameRate)));
            }

            if (parameterName == nameof(BinningVertical))
            {
                // Set binning in camera.
                LibWrapper.SetBinning(_cameraHandle, (ushort)BinningVertical, BinningOrientation.Vertical);

                var previousBinning = SensorHeight / HeightMax;
                var previousHeight = Height;
                var previousOffsetY = OffsetY;

                HeightMax.Value = SensorHeight / BinningVertical;
                if (previousBinning / BinningVertical < 1) // binning down
                {
                    Height.Value = previousHeight * previousBinning / BinningVertical;
                    Height.ImposeMax(HeightMax);
                    OffsetY.Value = previousOffsetY * previousBinning / BinningVertical;
                    OffsetY.ImposeMax(HeightMax - Height);
                }
                else // binning up
                {
                    Height.ImposeMax(HeightMax);
                    Height.Value = previousHeight * previousBinning / BinningVertical;
                    OffsetY.ImposeMax(HeightMax - Height);
                    OffsetY.Value = previousOffsetY * previousBinning / BinningVertical;
                }

                UpdateROI();

                // Retain frame rate.
                OnParameterChanged(this, new ParameterInvalidateEventArgs(nameof(AcquisitionFrameRate)));
            }

            if (parameterName == nameof(NoiseFilterMode))
            {
                try
                {
                    // Try setting trigger mode in camera.
                    LibWrapper.SetNoiseFilterMode(_cameraHandle, (NoiseFilterMode)NoiseFilterMode.IntValue);

                }
                catch (Exception)
                {
                    // Reset value.
                    NoiseFilterMode.IntValue = (long)LibWrapper.GetNoiseFilterMode(_cameraHandle);

                    throw;
                }
            }

            if (parameterName == nameof(AcquisitionFrameRate))
            {
                try
                {
                    LibWrapper.SetFrameRate(_cameraHandle, (float)AcquisitionFrameRate);

                    // Get updated exposure time.
                    ExposureTime.Value = LibWrapper.GetExposureTime(_cameraHandle);
                }
                catch (Exception)
                {
                    // Reset value.
                    AcquisitionFrameRate.Value = LibWrapper.GetFrameRate(_cameraHandle);

                    throw;
                }
            }

            if (parameterName == nameof(ExposureTime))
            {
                try
                {
                    // Try to set requested exposure time in camera.
                    LibWrapper.SetExposureTime(_cameraHandle, ExposureTime);

                    // Retrieve exposure time currently set.
                    ExposureTime.Value = LibWrapper.GetExposureTime(_cameraHandle);

                    // Retain frame rate.
                    OnParameterChanged(this, new ParameterInvalidateEventArgs(nameof(AcquisitionFrameRate)));
                }
                catch (Exception)
                {
                    // Reset value.
                    ExposureTime.Value = LibWrapper.GetExposureTime(_cameraHandle);

                    throw;
                }
            }

            if (parameterName == nameof(TriggerMode))
            {
                try
                {
                    // Try setting trigger mode in camera.
                    LibWrapper.SetTriggerMode(_cameraHandle, (TriggerMode)TriggerMode.IntValue);
                }
                catch (PcoException)
                {
                    // Reset value.
                    TriggerMode.IntValue = (long)LibWrapper.GetTriggerMode(_cameraHandle);

                    throw;
                }
            }

            if (parameterName == nameof(InputBufferCount))
            {
                SetBufferCapacity((uint)InputBufferCount.Value);
            }
        }

        /// <summary>
        /// Updates ROI in camera.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void UpdateROI()
        {
            try
            {
                // Change ROI in camera.
                LibWrapper.SetROI(_cameraHandle, (ushort)Width, (ushort)Height, (ushort)OffsetX, (ushort)OffsetY);

                // Validate ROI settings.
                LibWrapper.ArmCamera(_cameraHandle);
            }
            catch (PcoException ex)
            {
                // Read current ROI settings from camera.
                LibWrapper.GetROI(_cameraHandle, out ushort wRoiX0, out ushort wRoiY0, out ushort wRoiX1, out ushort  wRoiY1);

                // Revert properties to current camera ROI settings.
                OffsetX.Value = wRoiX0 - 1;
                OffsetY.Value = wRoiY0 - 1;
                Width.Value = wRoiX1 - OffsetX;
                Height.Value = wRoiY1 - OffsetY;

                throw new InvalidOperationException("Failed to update ROI in camera!", ex);
            }
        }
    }
}