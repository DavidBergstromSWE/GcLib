using System;
using GcLib.Utilities.Imaging;

namespace GcLib;

public sealed partial class VirtualCam
{
    private sealed partial class GenApi
    {
        /// <summary>
        /// Handles updates of derived parameter values and constraints.
        /// </summary>
        private sealed class DerivedParameterUpdater(GenApi owner)
        {
            private readonly GenApi _owner = owner;

            /// <summary>
            /// Updates derived parameters based on a changed source parameter.
            /// </summary>
            /// <param name="changedParameterName">Name of changed parameter. Null updates all derived values.</param>
            public void Update(string changedParameterName = null)
            {
                if (changedParameterName is null)
                {
                    UpdateGeometryForBinningHorizontal();
                    UpdateGeometryForBinningVertical();
                    UpdateImageGeometryConstraints();
                    UpdatePixelSize();
                    UpdatePayloadSize();
                    UpdateDeviceTemperature();
                    return;
                }

                if (changedParameterName == nameof(Width)
                    || changedParameterName == nameof(Height))
                {
                    UpdateImageGeometryConstraints();
                    UpdatePayloadSize();
                    return;
                }

                if (changedParameterName == nameof(BinningHorizontal))
                {
                    UpdateGeometryForBinningHorizontal();
                    UpdateImageGeometryConstraints();
                    UpdatePayloadSize();
                    return;
                }

                if (changedParameterName == nameof(BinningVertical))
                {
                    UpdateGeometryForBinningVertical();
                    UpdateImageGeometryConstraints();
                    UpdatePayloadSize();
                    return;
                }

                if (changedParameterName == nameof(PixelFormat))
                {
                    UpdatePixelSize();
                    UpdatePayloadSize();
                    return;
                }

                if (changedParameterName == nameof(DeviceTemperatureSelector))
                    UpdateDeviceTemperature();
            }

            /// <summary>
            /// Updates offset constraints from the current image width and height.
            /// </summary>
            private void UpdateImageGeometryConstraints()
            {
                _owner.OffsetX.ImposeMax(_owner.WidthMax - _owner.Width);
                _owner.OffsetY.ImposeMax(_owner.HeightMax - _owner.Height);
            }

            /// <summary>
            /// Updates width-domain geometry for the current horizontal binning value.
            /// </summary>
            private void UpdateGeometryForBinningHorizontal()
            {
                var requestedBinning = Math.Max(1L, _owner.BinningHorizontal.Value);
                var previousWidthMax = Math.Max(1L, _owner.WidthMax.Value);
                var previousBinning = Math.Max(1L, _owner.SensorWidth.Value / previousWidthMax);
                var previousWidth = _owner.Width.Value;
                var previousOffsetX = _owner.OffsetX.Value;

                _owner.WidthMax.Value = Math.Max(1L, _owner.SensorWidth.Value / requestedBinning);

                _owner.Width.ImposeMax(_owner.WidthMax.Value);
                _owner.Width.Value = Math.Max(_owner.Width.Min, previousWidth * previousBinning / requestedBinning);

                _owner.OffsetX.ImposeMax(_owner.WidthMax.Value - _owner.Width.Value);
                _owner.OffsetX.Value = Math.Max(_owner.OffsetX.Min, previousOffsetX * previousBinning / requestedBinning);
            }

            /// <summary>
            /// Updates height-domain geometry for the current vertical binning value.
            /// </summary>
            private void UpdateGeometryForBinningVertical()
            {
                var requestedBinning = Math.Max(1L, _owner.BinningVertical.Value);
                var previousHeightMax = Math.Max(1L, _owner.HeightMax.Value);
                var previousBinning = Math.Max(1L, _owner.SensorHeight.Value / previousHeightMax);
                var previousHeight = _owner.Height.Value;
                var previousOffsetY = _owner.OffsetY.Value;

                _owner.HeightMax.Value = Math.Max(1L, _owner.SensorHeight.Value / requestedBinning);

                _owner.Height.ImposeMax(_owner.HeightMax.Value);
                _owner.Height.Value = Math.Max(_owner.Height.Min, previousHeight * previousBinning / requestedBinning);

                _owner.OffsetY.ImposeMax(_owner.HeightMax.Value - _owner.Height.Value);
                _owner.OffsetY.Value = Math.Max(_owner.OffsetY.Min, previousOffsetY * previousBinning / requestedBinning);
            }

            /// <summary>
            /// Updates pixel-size enumeration from current pixel format.
            /// </summary>
            private void UpdatePixelSize()
            {
                _owner.PixelSize.IntValue = (int)GenICamHelper.GetPixelSize((PixelFormat)_owner.PixelFormat.IntValue);
            }

            /// <summary>
            /// Updates payload size from current output dimensions and bit depth.
            /// </summary>
            private void UpdatePayloadSize()
            {
                var bitsPerPixel = GenICamHelper.GetBitsPerPixel((PixelFormat)_owner.PixelFormat.IntValue);
                _owner.PayloadSize.Value = _owner.Width.Value * _owner.Height.Value * bitsPerPixel / 8;
            }

            /// <summary>
            /// Updates simulated device temperature based on selected measurement source.
            /// </summary>
            private void UpdateDeviceTemperature()
            {
                switch (_owner.DeviceTemperatureSelector.StringValue)
                {
                    case "Sensor":
                        _owner.DeviceTemperature.Value = 34.1;
                        break;

                    case "Mainboard":
                        _owner.DeviceTemperature.Value = 23.3;
                        break;

                    case "DeviceSpecific":
                        _owner.DeviceTemperature.Value = 20.0;
                        break;
                }
            }
        }
    }
}
