namespace GcLib;

#region GenICam Enumerations

/// <summary>
/// Type of increment.
/// </summary>
public enum EIncMode
{
    /// <summary>
    /// No increments.
    /// </summary>
    noIncrement,
    /// <summary>
    /// Fixed range between increments.
    /// </summary>
    fixedIncrement,
    /// <summary>
    /// List of increments.
    /// </summary>
    listIncrement
}

//public enum EDisplayNotation { fnAutomatic, fnFixed, fnScientific, _UndefinedEDisplayNotation }
//public enum ERepresentation { Linear, Logarithmic, Boolean, PureNumber, HexNumber, IPV4Address, MACAddress, _UndefinedRepresentation }

/// <summary>
/// Transport layer type of the device.
/// </summary>
public enum DeviceTLType
{
    /// <summary>
    /// GigE Vision.
    /// </summary>
    GigEVision,
    /// <summary>
    /// Camera Link.
    /// </summary>
    CameraLink,
    /// <summary>
    /// Camera Link High Speed.
    /// </summary>
    CameraLinkHS,
    /// <summary>
    /// CoaXPress.
    /// </summary>
    CoaXPress,
    /// <summary>
    /// USB3 Vision.
    /// </summary>
    USB3Vision,
    /// <summary>
    /// USB.
    /// </summary>
    USB,
    /// <summary>
    /// Custom Transport Layer.
    /// </summary>
    Custom
}

/// <summary>
/// Location for temperature measurement within camera.
/// </summary>
public enum DeviceTemperatureSelector
{
    /// <summary>
    /// Temperature of the image sensor of the camera.
    /// </summary>
    Sensor = 0,
    /// <summary>
    /// Temperature of the device's mainboard.
    /// </summary>
    Mainboard,
    /// <summary>
    /// Device-specific temperature location.
    /// </summary>
    DeviceSpecific
}

//public enum DeviceSerialPortBaudRate { Baud9600 = 1, Baud19200, Baud38400, Baud57600, Baud115200, Baud230400, Baud460800, Baud921600 }; // baud rate of serial connection

/// <summary>
/// Horizontal binning mode.
/// </summary>
public enum BinningHorizontalMode
{
    /// <summary>
    /// The response from the combined cells will be added, resulting in increased sensitivity.
    /// </summary>
    Sum,
    /// <summary>
    /// The response from the combined cells will be averaged, resulting in increased signal/noise ratio.
    /// </summary>
    Average
}

/// <summary>
/// Vertical binning mode.
/// </summary>
public enum BinningVerticalMode
{
    /// <summary>
    /// The response from the combined cells will be added, resulting in increased sensitivity.
    /// </summary>
    Sum,
    /// <summary>
    /// The response from the combined cells will be averaged, resulting in increased signal/noise ratio.
    /// </summary>
    Average
}

/// <summary>
/// Mode used to reduce the horizontal resolution.
/// </summary>
public enum DecimationHorizontalMode
{
    /// <summary>
    /// The value of every Nth pixel is kept, others are discarded.
    /// </summary>
    Discard,
    /// <summary>
    /// The values of a group of N adjacent pixels are averaged.
    /// </summary>
    Average
}

/// <summary>
/// Mode used to reduce the horizontal resolution.
/// </summary>
public enum DecimationVerticalMode
{
    /// <summary>
    /// The value of every Nth pixel is kept, others are discarded.
    /// </summary>
    Discard,
    /// <summary>
    /// The values of a group of N adjacent pixels are averaged.
    /// </summary>
    Average
}

/// <summary>
/// Total size in bits of a pixel of the image.
/// </summary>
public enum PixelSize
{
    /// <summary>
    /// 1 bit per pixel.
    /// </summary>
    Bpp1 = 1,
    /// <summary>
    /// 2 bits per pixel.
    /// </summary>
    Bpp2 = 2,
    /// <summary>
    /// 4 bits per pixel.
    /// </summary>
    Bpp4 = 4,
    /// <summary>
    /// 8 bits per pixel.
    /// </summary>
    Bpp8 = 8,
    /// <summary>
    /// 10 bits per pixel.
    /// </summary>
    Bpp10 = 10,
    /// <summary>
    /// 12 bits per pixel.
    /// </summary>
    Bpp12 = 12,
    /// <summary>
    /// 14 bits per pixel.
    /// </summary>
    Bpp14 = 14,
    /// <summary>
    /// 16 bits per pixel.
    /// </summary>
    Bpp16 = 16,
    /// <summary>
    /// 32 bits per pixel.
    /// </summary>
    Bpp32 = 32,
    /// <summary>
    /// 64 bits per pixel.
    /// </summary>
    Bpp64 = 64,
    /// <summary>
    /// 96 bits per pixel.
    /// </summary>
    Bpp96 = 96
}

/// <summary>
/// Types of test patterns.
/// </summary>
public enum TestPattern
{
    /// <summary>
    /// Image is coming from the sensor.
    /// </summary>
    Off,
    /// <summary>
    /// Image is filled with the darkest possible image.
    /// </summary>
    Black,
    /// <summary>
    /// Image is filled with the brightest possible image.
    /// </summary>
    White,
    /// <summary>
    /// Image is filled vertically with an image that goes from the darkest possible value to the brightest.
    /// </summary>
    GrayVerticalRamp,
    /// <summary>
    /// Image is filled vertically with an image that goes from the darkest possible value to the brightest and that moves verticaly from top to bottom at each frame.
    /// </summary>
    GrayVerticalRampMoving,
    /// <summary>
    /// Image is filled horizontally with an image that goes from the darkest possible value to the brightest.
    /// </summary>
    GrayHorizontalRamp,
    /// <summary>
    /// Image is filled horizontally with an image that goes from the darkest possible value to the brightest and that moves horizontally from left to right at each frame.
    /// </summary>
    GrayHorizontalRampMoving,
    /// <summary>
    /// Image shows a moving horizontal line.
    /// </summary>
    HorizontalLineMoving,
    /// <summary>
    /// Image shows a moving vertical line.
    /// </summary>
    VerticalLineMoving,
    /// <summary>
    /// Image is cycled in uniform gray tones with frame counter superimposed in image center.
    /// </summary>
    FrameCounter,
    /// <summary>
    /// Images are taken from a directory specified.
    /// </summary>
    ImageDirectory,
    /// <summary>
    /// Image with white noise.
    /// </summary>
    WhiteNoise,
    /// <summary>
    /// Image with uniform red color.
    /// </summary>
    Red,
    /// <summary>
    /// Image with uniform green color.
    /// </summary>
    Green,
    /// <summary>
    /// Image with uniform blue color.
    /// </summary>
    Blue
}

/// <summary>
/// Image compression modes.
/// </summary>
public enum ImageCompressionMode
{
    /// <summary>
    /// Default value. Image compression is disabled. Images are transmitted uncompressed.
    /// </summary>
    Off,
    /// <summary>
    /// JPEG compression is selected.
    /// </summary>
    JPEG,
    /// <summary>
    /// JPEG 2000 compression is selected.
    /// </summary>
    JPEG2000,
    /// <summary>
    /// H.264 compression is selected.
    /// </summary>
    H264,
    /// <summary>
    /// H.265 compression is selected.
    /// </summary>
    H265
}

/// <summary>
/// Acquisition modes of the device. It defines mainly the number of frames to capture during an acquisition and the way the acquisition stops.
/// </summary>
public enum AcquisitionMode
{
    /// <summary>
    /// One frame is captured.
    /// </summary>
    SingleFrame,
    /// <summary>
    /// The number of frames specified by AcquisitionFrameCount is captured.
    /// </summary>
    MultiFrame,
    /// <summary>
    /// Frames are captured continuously until stopped with the AcquisitionStop command.
    /// </summary>
    Continuous
}

/// <summary>
/// Automatic exposure modes.
/// </summary>
public enum ExposureAuto
{
    /// <summary>
    /// Exposure duration is user controlled using ExposureTime.
    /// </summary>
    Off,
    /// <summary>
    /// Exposure duration is adapted once by the device. Once it has converged, it returns to the Off state.
    /// </summary>
    Once,
    /// <summary>
    /// Exposure duration is constantly adapted by the device to maximize the dynamic range.
    /// </summary>
    Continuous
}

/// <summary>
/// Automatic gain control (AGC) mode.
/// </summary>
public enum GainAuto
{
    /// <summary>
    /// Gain is User controlled using Gain.
    /// </summary>
    Off,
    /// <summary>
    /// Gain is automatically adjusted once by the device. Once it has converged, it automatically returns to the Off state.
    /// </summary>
    Once,
    /// <summary>
    /// Gain is constantly adjusted by the device.
    /// </summary>
    Continuous
}

/// <summary>
/// Mode for automatic white balancing between the color channels.
/// </summary>
public enum BalanceWhiteAuto
{
    /// <summary>
    /// White balancing is user controlled using BalanceRatioSelector and BalanceRatio.
    /// </summary>
    Off,
    /// <summary>
    /// White balancing is automatically adjusted once by the device. Once it has converged, it automatically returns to the Off state.
    /// </summary>
    Once,
    /// <summary>
    /// White balancing is constantly adjusted by the device.
    /// </summary>
    Continuous
}

/// <summary>
/// Type of Bayer color filter array applied to an image.
/// </summary>
public enum PixelColorFilter
{
    /// <summary>
    /// No color filter array applied.
    /// </summary>
    None = 0,
    /// <summary>
    /// Pixels filtered to red (0,0), green (0,1), green (1,0) and blue (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Red Green</item>
    /// <item>Green Blue</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerRGGB,
    /// <summary>
    /// Pixels filtered to green (0,0), blue (0,1), red (1,0) and green (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Green Blue</item>
    /// <item>Red Green</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerGBRG,
    /// <summary>
    /// Pixels filtered to green (0,0), red (0,1), blue (1,0) and green (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Green Red</item>
    /// <item>Blue Green</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerGRBG,
    /// <summary>
    /// Pixels filtered to blue (0,0), green (0,1), green (1,0) and red (1,1) in a 2 x 2 pixel neighbourhood (as defined by the upper-left corner of the image).
    /// <i>
    /// <list type="table">
    /// <item>Blue Green</item>
    /// <item>Green Red</item>
    /// </list>
    /// </i>
    /// </summary>
    BayerBGGR
}

/// <summary>
/// Pixel formats available in GenICam Pixel Format Naming Convention (PFNC) version 2.3.
/// </summary>
public enum PixelFormat
{
    /// <summary>
    /// Monochrome 1-bit packed.
    /// </summary>
    Mono1p = 0x01010037,
    /// <summary>
    /// Monochrome 2-bit packed.
    /// </summary>
    Mono2p = 0x01020038,
    /// <summary>
    /// Monochrome 4-bit packed.
    /// </summary>
    Mono4p = 0x01040039,
    /// <summary>
    /// Monochrome 8-bit.
    /// </summary>
    Mono8 = 0x01080001,
    /// <summary>
    /// Monochrome 8-bit signed.
    /// </summary>
    Mono8s = 0x01080002,
    /// <summary>
    /// Monochrome 10-bit unpacked.
    /// </summary>
    Mono10 = 0x01100003,
    /// <summary>
    /// Monochrome 10-bit packed.
    /// </summary>
    Mono10p = 0x010A0046,
    /// <summary>
    /// Monochrome 12-bit unpacked.
    /// </summary>
    Mono12 = 0x01100005,
    /// <summary>
    /// Monochrome 12-bit packed.
    /// </summary>
    Mono12p = 0x010C0047,
    /// <summary>
    /// Monochrome 14-bit unpacked.
    /// </summary>
    Mono14 = 0x01100025,
    /// <summary>
    /// Monochrome 14-bit packed.
    /// </summary>
    Mono14p = 0x010E0104,
    /// <summary>
    /// Monochrome 16-bit.
    /// </summary>
    Mono16 = 0x01100007,
    /// <summary>
    /// Monochrome 32-bit.
    /// </summary>
    Mono32 = 0x01200111,
    /// <summary>
    /// Bayer Blue-Green 4-bit packed.
    /// </summary>
    BayerBG4p = 0x01040110,
    /// <summary>
    /// Bayer Blue-Green 8-bit.
    /// </summary>
    BayerBG8 = 0x0108000B,
    /// <summary>
    /// Bayer Blue-Green 10-bit unpacked.
    /// </summary>
    BayerBG10 = 0x0110000F,
    /// <summary>
    /// Bayer Blue-Green 10-bit packed.
    /// </summary>
    BayerBG10p = 0x010A0052,
    /// <summary>
    /// Bayer Blue-Green 12-bit unpacked.
    /// </summary>
    BayerBG12 = 0x01100013,
    /// <summary>
    /// Bayer Blue-Green 12-bit packed.
    /// </summary>
    BayerBG12p = 0x010C0053,
    /// <summary>
    /// Bayer Blue-Green 14-bit.
    /// </summary>
    BayerBG14 = 0x0110010C,
    /// <summary>
    /// Bayer Blue-Green 14-bit packed.
    /// </summary>
    BayerBG14p = 0x010E0108,
    /// <summary>
    /// Bayer Blue-Green 16-bit.
    /// </summary>
    BayerBG16 = 0x01100031,
    /// <summary>
    /// Bayer Green-Blue 4-bit packed.
    /// </summary>
    BayerGB4p = 0x0104010F,
    /// <summary>
    /// Bayer Green-Blue 8-bit.
    /// </summary>
    BayerGB8 = 0x0108000A,
    /// <summary>
    /// Bayer Green-Blue 10-bit unpacked.
    /// </summary>
    BayerGB10 = 0x0110000E,
    /// <summary>
    /// Bayer Green-Blue 10-bit packed.
    /// </summary>
    BayerGB10p = 0x010A0054,
    /// <summary>
    /// Bayer Green-Blue 12-bit unpacked.
    /// </summary>
    BayerGB12 = 0x01100012,
    /// <summary>
    /// Bayer Green-Blue 12-bit packed.
    /// </summary>
    BayerGB12p = 0x010C0055,
    /// <summary>
    /// Bayer Green-Blue 14-bit.
    /// </summary>
    BayerGB14 = 0x0110010B,
    /// <summary>
    /// Bayer Green-Blue 14-bit packed.
    /// </summary>
    BayerGB14p = 0x010E0107,
    /// <summary>
    /// Bayer Green-Blue 16-bit.
    /// </summary>
    BayerGB16 = 0x01100030,
    /// <summary>
    /// Bayer Green-Red 4-bit packed.
    /// </summary>
    BayerGR4p = 0x0104010D,
    /// <summary>
    /// Bayer Green-Red 8-bit.
    /// </summary>
    BayerGR8 = 0x01080008,
    /// <summary>
    /// Bayer Green-Red 10-bit unpacked.
    /// </summary>
    BayerGR10 = 0x0110000C,
    /// <summary>
    /// Bayer Green-Red 10-bit packed.
    /// </summary>
    BayerGR10p = 0x010A0056,
    /// <summary>
    /// Bayer Green-Red 12-bit unpacked
    /// </summary>
    BayerGR12 = 0x01100010,
    /// <summary>
    /// Bayer Green-Red 12-bit packed.
    /// </summary>
    BayerGR12p = 0x010C0057,
    /// <summary>
    /// Bayer Green-Red 14-bit.
    /// </summary>
    BayerGR14 = 0x01100109,
    /// <summary>
    /// Bayer Green-Red 14-bit packed.
    /// </summary>
    BayerGR14p = 0x010E0105,
    /// <summary>
    /// Bayer Green-Red 16-bit.
    /// </summary>
    BayerGR16 = 0x0110002E,
    /// <summary>
    /// Bayer Red-Green 4-bit packed.
    /// </summary>
    BayerRG4p = 0x0104010E,
    /// <summary>
    /// Bayer Red-Green 8-bit.
    /// </summary>
    BayerRG8 = 0x01080009,
    /// <summary>
    /// Bayer Red-Green 10-bit unpacked.
    /// </summary>
    BayerRG10 = 0x0110000D,
    /// <summary>
    /// Bayer Red-Green 10-bit packed.
    /// </summary>
    BayerRG10p = 0x010A0058,
    /// <summary>
    /// Bayer Red-Green 12-bit unpacked.
    /// </summary>
    BayerRG12 = 0x01100011,
    /// <summary>
    /// Bayer Red-Green 12-bit packed.
    /// </summary>
    BayerRG12p = 0x010C0059,
    /// <summary>
    /// Bayer Red-Green 14-bit.
    /// </summary>
    BayerRG14 = 0x0110010A,
    /// <summary>
    /// Bayer Red-Green 14-bit packed.
    /// </summary>
    BayerRG14p = 0x010E0106,
    /// <summary>
    /// Bayer Red-Green 16-bit.
    /// </summary>
    BayerRG16 = 0x0110002F,
    /// <summary>
    /// Red-Green-Blue-alpha 8-bit.
    /// </summary>
    RGBa8 = 0x02200016,
    /// <summary>
    /// Red-Green-Blue-alpha 10-bit unpacked.
    /// </summary>
    RGBa10 = 0x0240005F,
    /// <summary>
    /// Red-Green-Blue-alpha 10-bit packed.
    /// </summary>
    RGBa10p = 0x02280060,
    /// <summary>
    /// Red-Green-Blue-alpha 12-bit unpacked.
    /// </summary>
    RGBa12 = 0x02400061,
    /// <summary>
    /// Red-Green-Blue-alpha 12-bit packed.
    /// </summary>
    RGBa12p = 0x02300062,
    /// <summary>
    /// Red-Green-Blue-alpha 14-bit unpacked.
    /// </summary>
    RGBa14 = 0x02400063,
    /// <summary>
    /// Red-Green-Blue-alpha 16-bit.
    /// </summary>
    RGBa16 = 0x02400064,
    /// <summary>
    /// Red-Green-Blue 8-bit.
    /// </summary>
    RGB8 = 0x02180014,
    /// <summary>
    /// Red-Green-Blue 8-bit planar.
    /// </summary>
    RGB8_Planar = 0x02180021,
    /// <summary>
    /// Red-Green-Blue 10-bit unpacked.
    /// </summary>
    RGB10 = 0x02300018,
    /// <summary>
    /// Red-Green-Blue 10-bit unpacked planar.
    /// </summary>
    RGB10_Planar = 0x02300022,
    /// <summary>
    /// Red-Green-Blue 10-bit packed.
    /// </summary>
    RGB10p = 0x021E005C,
    /// <summary>
    /// Red-Green-Blue 10-bit packed into 32-bit.
    /// </summary>
    RGB10p32 = 0x0220001D,
    /// <summary>
    /// Red-Green-Blue 12-bit unpacked.
    /// </summary>
    RGB12 = 0x0230001A,
    /// <summary>
    /// Red-Green-Blue 12-bit unpacked planar.
    /// </summary>
    RGB12_Planar = 0x02300023,
    /// <summary>
    /// Red-Green-Blue 12-bit packed.
    /// </summary>
    RGB12p = 0x0224005D,
    /// <summary>
    /// Red-Green-Blue 14-bit unpacked.
    /// </summary>
    RGB14 = 0x0230005E,
    /// <summary>
    /// Red-Green-Blue 16-bit.
    /// </summary>
    RGB16 = 0x02300033,
    /// <summary>
    /// Red-Green-Blue 16-bit planar.
    /// </summary>
    RGB16_Planar = 0x02300024,
    /// <summary>
    /// Red-Green-Blue 5/6/5-bit packed.
    /// </summary>
    RGB565p = 0x02100035,
    /// <summary>
    /// Blue-Green-Red-alpha 8-bit.
    /// </summary>
    BGRa8 = 0x02200017,
    /// <summary>
    /// Blue-Green-Red-alpha 10-bit unpacked.
    /// </summary>
    BGRa10 = 0x0240004C,
    /// <summary>
    /// Blue-Green-Red-alpha 10-bit packed.
    /// </summary>
    BGRa10p = 0x0228004D,
    /// <summary>
    /// Blue-Green-Red-alpha 12-bit unpacked.
    /// </summary>
    BGRa12 = 0x0240004E,
    /// <summary>
    /// Blue-Green-Red-alpha 12-bit packed.
    /// </summary>
    BGRa12p = 0x0230004F,
    /// <summary>
    /// Blue-Green-Red-alpha 14-bit unpacked.
    /// </summary>
    BGRa14 = 0x02400050,
    /// <summary>
    /// Blue-Green-Red-alpha 16-bit.
    /// </summary>
    BGRa16 = 0x02400051,
    /// <summary>
    /// Blue-Green-Red 8-bit.
    /// </summary>
    BGR8 = 0x02180015,
    /// <summary>
    /// Blue-Green-Red 10-bit unpacked.
    /// </summary>
    BGR10 = 0x02300019,
    /// <summary>
    /// Blue-Green-Red 10-bit packed.
    /// </summary>
    BGR10p = 0x021E0048,
    /// <summary>
    /// Blue-Green-Red 12-bit unpacked.
    /// </summary>
    BGR12 = 0x0230001B,
    /// <summary>
    /// Blue-Green-Red 12-bit packed.
    /// </summary>
    BGR12p = 0x02240049,
    /// <summary>
    /// Blue-Green-Red 14-bit unpacked.
    /// </summary>
    BGR14 = 0x0230004A,
    /// <summary>
    /// Blue-Green-Red 16-bit.
    /// </summary>
    BGR16 = 0x0230004B,
    /// <summary>
    /// Blue-Green-Red 5/6/5-bit packed.
    /// </summary>
    BGR565p = 0x02100036,
    /// <summary>
    /// Red 8-bit.
    /// </summary>
    R8 = 0x010800C9,
    /// <summary>
    /// Red 10-bit.
    /// </summary>
    R10 = 0x010A00CA,
    /// <summary>
    /// Red 12-bit.
    /// </summary>
    R12 = 0x010C00CB,
    /// <summary>
    /// Red 16-bit.
    /// </summary>
    R16 = 0x011000CC,
    /// <summary>
    /// Green 8-bit.
    /// </summary>
    G8 = 0x010800CD,
    /// <summary>
    /// Green 10-bit.
    /// </summary>
    G10 = 0x010A00CE,
    /// <summary>
    /// Green 12-bit.
    /// </summary>
    G12 = 0x010C00CF,
    /// <summary>
    /// Green 16-bit.
    /// </summary>
    G16 = 0x011000D0,
    /// <summary>
    /// Blue 8-bit.
    /// </summary>
    B8 = 0x010800D1,
    /// <summary>
    /// Blue 10-bit.
    /// </summary>
    B10 = 0x010A00D2,
    /// <summary>
    /// Blue 12-bit.
    /// </summary>
    B12 = 0x010C00D3,
    /// <summary>
    /// Blue 16-bit.
    /// </summary>
    B16 = 0x011000D4,
    /// <summary>
    /// 3D coordinate A-B-C 8-bit.
    /// </summary>
    Coord3D_ABC8 = 0x021800B2,
    /// <summary>
    /// 3D coordinate A-B-C 8-bit planar.
    /// </summary>
    Coord3D_ABC8_Planar = 0x021800B3,
    /// <summary>
    /// 3D coordinate A-B-C 10-bit packed.
    /// </summary>
    Coord3D_ABC10p = 0x021E00DB,
    /// <summary>
    /// 3D coordinate A-B-C 10-bit packed planar.
    /// </summary>
    Coord3D_ABC10p_Planar = 0x021E00DC,
    /// <summary>
    /// 3D coordinate A-B-C 12-bit packed.
    /// </summary>
    Coord3D_ABC12p = 0x022400DE,
    /// <summary>
    /// 3D coordinate A-B-C 12-bit packed planar.
    /// </summary>
    Coord3D_ABC12p_Planar = 0x022400DF,
    /// <summary>
    /// 3D coordinate A-B-C 16-bit.
    /// </summary>
    Coord3D_ABC16 = 0x023000B9,
    /// <summary>
    /// 3D coordinate A-B-C 16-bit planar.
    /// </summary>
    Coord3D_ABC16_Planar = 0x023000BA,
    /// <summary>
    /// 3D coordinate A-B-C 32-bit floating point.
    /// </summary>
    Coord3D_ABC32f = 0x026000C0,
    /// <summary>
    /// 3D coordinate A-B-C 32-bit floating point planar.
    /// </summary>
    Coord3D_ABC32f_Planar = 0x026000C1,
    /// <summary>
    /// 3D coordinate A-C 8-bit.
    /// </summary>
    Coord3D_AC8 = 0x021000B4,
    /// <summary>
    /// 3D coordinate A-C 8-bit planar.
    /// </summary>
    Coord3D_AC8_Planar = 0x021000B5,
    /// <summary>
    /// 3D coordinate A-C 10-bit packed.
    /// </summary>
    Coord3D_AC10p = 0x021400F0,
    /// <summary>
    /// 3D coordinate A-C 10-bit packed planar.
    /// </summary>
    Coord3D_AC10p_Planar = 0x021400F1,
    /// <summary>
    /// 3D coordinate A-C 12-bit packed.
    /// </summary>
    Coord3D_AC12p = 0x021800F2,
    /// <summary>
    /// 3D coordinate A-C 12-bit packed planar.
    /// </summary>
    Coord3D_AC12p_Planar = 0x021800F3,
    /// <summary>
    /// 3D coordinate A-C 16-bit.
    /// </summary>
    Coord3D_AC16 = 0x022000BB,
    /// <summary>
    /// 3D coordinate A-C 16-bit planar.
    /// </summary>
    Coord3D_AC16_Planar = 0x022000BC,
    /// <summary>
    /// 3D coordinate A-C 32-bit floating point.
    /// </summary>
    Coord3D_AC32f = 0x024000C2,
    /// <summary>
    /// 3D coordinate A-C 32-bit floating point planar.
    /// </summary>
    Coord3D_AC32f_Planar = 0x024000C3,
    /// <summary>
    /// 3D coordinate A 8-bit.
    /// </summary>
    Coord3D_A8 = 0x010800AF,
    /// <summary>
    /// 3D coordinate A 10-bit packed.
    /// </summary>
    Coord3D_A10p = 0x010A00D5,
    /// <summary>
    /// 3D coordinate A 12-bit packed.
    /// </summary>
    Coord3D_A12p = 0x010C00D8,
    /// <summary>
    /// 3D coordinate A 16-bit.
    /// </summary>
    Coord3D_A16 = 0x011000B6,
    /// <summary>
    /// 3D coordinate A 32-bit floating point.
    /// </summary>
    Coord3D_A32f = 0x012000BD,
    /// <summary>
    /// 3D coordinate B 8-bit.
    /// </summary>
    Coord3D_B8 = 0x010800B0,
    /// <summary>
    /// 3D coordinate B 10-bit packed.
    /// </summary>
    Coord3D_B10p = 0x010A00D6,
    /// <summary>
    /// 3D coordinate B 12-bit packed.
    /// </summary>
    Coord3D_B12p = 0x010C00D9,
    /// <summary>
    /// 3D coordinate B 16-bit.
    /// </summary>
    Coord3D_B16 = 0x011000B7,
    /// <summary>
    /// 3D coordinate B 32-bit floating point.
    /// </summary>
    Coord3D_B32f = 0x012000BE,
    /// <summary>
    /// 3D coordinate C 8-bit.
    /// </summary>
    Coord3D_C8 = 0x010800B1,
    /// <summary>
    /// 3D coordinate C 10-bit packed.
    /// </summary>
    Coord3D_C10p = 0x010A00D7,
    /// <summary>
    /// 3D coordinate C 12-bit packed.
    /// </summary>
    Coord3D_C12p = 0x010C00DA,
    /// <summary>
    /// 3D coordinate C 16-bit.
    /// </summary>
    Coord3D_C16 = 0x011000B8,
    /// <summary>
    /// 3D coordinate C 32-bit floating point.
    /// </summary>
    Coord3D_C32f = 0x012000BF,
    /// <summary>
    /// Confidence 1-bit unpacked.
    /// </summary>
    Confidence1 = 0x010800C4,
    /// <summary>
    /// Confidence 1-bit packed.
    /// </summary>
    Confidence1p = 0x010100C5,
    /// <summary>
    /// Confidence 8-bit.
    /// </summary>
    Confidence8 = 0x010800C6,
    /// <summary>
    /// Confidence 16-bit.
    /// </summary>
    Confidence16 = 0x011000C7,
    /// <summary>
    /// Confidence 32-bit floating point.
    /// </summary>
    Confidence32f = 0x012000C8,
    /// <summary>
    /// Bi-color Blue/Green - Red/Green 8-bit.
    /// </summary>
    BiColorBGRG8 = 0x021000A6,
    /// <summary>
    /// Bi-color Blue/Green - Red/Green 10-bit unpacked.
    /// </summary>
    BiColorBGRG10 = 0x022000A9,
    /// <summary>
    /// Bi-color Blue/Green - Red/Green 10-bit packed.
    /// </summary>
    BiColorBGRG10p = 0x021400AA,
    /// <summary>
    /// Bi-color Blue/Green - Red/Green 12-bit unpacked.
    /// </summary>
    BiColorBGRG12 = 0x022000AD,
    /// <summary>
    /// Bi-color Blue/Green - Red/Green 12-bit packed.
    /// </summary>
    BiColorBGRG12p = 0x021800AE,
    /// <summary>
    /// Bi-color Red/Green - Blue/Green 8-bit.
    /// </summary>
    BiColorRGBG8 = 0x021000A5,
    /// <summary>
    /// Bi-color Red/Green - Blue/Green 10-bit unpacked.
    /// </summary>
    BiColorRGBG10 = 0x022000A7,
    /// <summary>
    /// Bi-color Red/Green - Blue/Green 10-bit packed.
    /// </summary>
    BiColorRGBG10p = 0x021400A8,
    /// <summary>
    /// Bi-color Red/Green - Blue/Green 12-bit unpacked.
    /// </summary>
    BiColorRGBG12 = 0x022000AB,
    /// <summary>
    /// Bi-color Red/Green - Blue/Green 12-bit packed.
    /// </summary>
    BiColorRGBG12p = 0x021800AC,
    /// <summary>
    /// Data 8-bit.
    /// </summary>
    Data8 = 0x01080116,
    /// <summary>
    /// Data 8-bit signed.
    /// </summary>
    Data8s = 0x01080117,
    /// <summary>
    /// Data 16-bit.
    /// </summary>
    Data16 = 0x01100118,
    /// <summary>
    /// Data 16-bit signed.
    /// </summary>
    Data16s = 0x01100119,
    /// <summary>
    /// Data 32-bit.
    /// </summary>
    Data32 = 0x0120011A,
    /// <summary>
    /// Data 32-bit floating point.
    /// </summary>
    Data32f = 0x0120011C,
    /// <summary>
    /// Data 32-bit signed.
    /// </summary>
    Data32s = 0x0120011B,
    /// <summary>
    /// Data 64-bit.
    /// </summary>
    Data64 = 0x0140011D,
    /// <summary>
    /// Data 64-bit floating point.
    /// </summary>
    Data64f = 0x0140011F,
    /// <summary>
    /// Data 64-bit signed.
    /// </summary>
    Data64s = 0x0140011E,
    /// <summary>
    /// Sparse Color Filter #1 White-Blue-White-Green 8-bit.
    /// </summary>
    SCF1WBWG8 = 0x01080067,
    /// <summary>
    /// Sparse Color Filter #1 White-Blue-White-Green 10-bit unpacked.
    /// </summary>
    SCF1WBWG10 = 0x01100068,
    /// <summary>
    /// Sparse Color Filter #1 White-Blue-White-Green 10-bit packed.
    /// </summary>
    SCF1WBWG10p = 0x010A0069,
    /// <summary>
    /// Sparse Color Filter #1 White-Blue-White-Green 12-bit unpacked.
    /// </summary>
    SCF1WBWG12 = 0x0110006A,
    /// <summary>
    /// Sparse Color Filter #1 White-Blue-White-Green 12-bit packed.
    /// </summary>
    SCF1WBWG12p = 0x010C006B,
    /// <summary>
    /// Sparse Color Filter #1 White-Blue-White-Green 14-bit unpacked.
    /// </summary>
    SCF1WBWG14 = 0x0110006C,
    /// <summary>
    /// Sparse Color Filter #1 White-Blue-White-Green 16-bit unpacked.
    /// </summary>
    SCF1WBWG16 = 0x0110006D,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Blue 8-bit.
    /// </summary>
    SCF1WGWB8 = 0x0108006E,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Blue 10-bit unpacked.
    /// </summary>
    SCF1WGWB10 = 0x0110006F,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Blue 10-bit packed.
    /// </summary>
    SCF1WGWB10p = 0x010A0070,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Blue 12-bit unpacked.
    /// </summary>
    SCF1WGWB12 = 0x01100071,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Blue 12-bit packed.
    /// </summary>
    SCF1WGWB12p = 0x010C0072,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Blue 14-bit unpacked.
    /// </summary>
    SCF1WGWB14 = 0x01100073,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Blue 16-bit.
    /// </summary>
    SCF1WGWB16 = 0x01100074,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Red 8-bit.
    /// </summary>
    SCF1WGWR8 = 0x01080075,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Red 10-bit unpacked.
    /// </summary>
    SCF1WGWR10 = 0x01100076,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Red 10-bit packed.
    /// </summary>
    SCF1WGWR10p = 0x010A0077,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Red 12-bit unpacked.
    /// </summary>
    SCF1WGWR12 = 0x01100078,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Red 12-bit packed.
    /// </summary>
    SCF1WGWR12p = 0x010C0079,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Red 14-bit unpacked.
    /// </summary>
    SCF1WGWR14 = 0x0110007A,
    /// <summary>
    /// Sparse Color Filter #1 White-Green-White-Red 16-bit.
    /// </summary>
    SCF1WGWR16 = 0x0110007B,
    /// <summary>
    /// Sparse Color Filter #1 White-Red-White-Green 8-bit.
    /// </summary>
    SCF1WRWG8 = 0x0108007C,
    /// <summary>
    /// Sparse Color Filter #1 White-Red-White-Green 10-bit unpacked.
    /// </summary>
    SCF1WRWG10 = 0x0110007D,
    /// <summary>
    /// Sparse Color Filter #1 White-Red-White-Green 10-bit packed.
    /// </summary>
    SCF1WRWG10p = 0x010A007E,
    /// <summary>
    /// Sparse Color Filter #1 White-Red-White-Green 12-bit unpacked.
    /// </summary>
    SCF1WRWG12 = 0x0110007F,
    /// <summary>
    /// Sparse Color Filter #1 White-Red-White-Green 12-bit packed.
    /// </summary>
    SCF1WRWG12p = 0x010C0080,
    /// <summary>
    /// Sparse Color Filter #1 White-Red-White-Green 14-bit unpacked.
    /// </summary>
    SCF1WRWG14 = 0x01100081,
    /// <summary>
    /// Sparse Color Filter #1 White-Red-White-Green 16-bit.
    /// </summary>
    SCF1WRWG16 = 0x01100082,
    /// <summary>
    /// YCbCr 4:4:4 8-bit.
    /// </summary>
    YCbCr8 = 0x0218005B,
    /// <summary>
    /// YCbCr 4:4:4 8-bit.
    /// </summary>
    YCbCr8_CbYCr = 0x0218003A,
    /// <summary>
    /// YCbCr 4:4:4 10-bit unpacked.
    /// </summary>
    YCbCr10_CbYCr = 0x02300083,
    /// <summary>
    /// YCbCr 4:4:4 10-bit packed.
    /// </summary>
    YCbCr10p_CbYCr = 0x021E0084,
    /// <summary>
    /// YCbCr 4:4:4 12-bit unpacked.
    /// </summary>
    YCbCr12_CbYCr = 0x02300085,
    /// <summary>
    /// YCbCr 4:4:4 12-bit packed.
    /// </summary>
    YCbCr12p_CbYCr = 0x02240086,
    /// <summary>
    /// YCbCr 4:1:1 8-bit.
    /// </summary>
    YCbCr411_8 = 0x020C005A,
    /// <summary>
    /// YCbCr 4:1:1 8-bit.
    /// </summary>
    YCbCr411_8_CbYYCrYY = 0x020C003C,
    /// <summary>
    /// YCbCr 4:2:0 8-bit YY/CbCr Semiplanar.
    /// </summary>
    YCbCr420_8_YY_CbCr_Semiplanar = 0x020C0112,
    /// <summary>
    /// YCbCr 4:2:0 8-bit YY/CrCb Semiplanar.
    /// </summary>
    YCbCr420_8_YY_CrCb_Semiplanar = 0x020C0114,
    /// <summary>
    /// YCbCr 4:2:2 8-bit.
    /// </summary>
    YCbCr422_8 = 0x0210003B,
    /// <summary>
    /// YCbCr 4:2:2 8-bit.
    /// </summary>
    YCbCr422_8_CbYCrY = 0x02100043,
    /// <summary>
    /// YCbCr 4:2:2 8-bit YY/CbCr Semiplanar.
    /// </summary>
    YCbCr422_8_YY_CbCr_Semiplanar = 0x02100113,
    /// <summary>
    /// YCbCr 4:2:2 8-bit YY/CrCb Semiplanar.
    /// </summary>
    YCbCr422_8_YY_CrCb_Semiplanar = 0x02100115,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked.
    /// </summary>
    YCbCr422_10 = 0x02200065,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked.
    /// </summary>
    YCbCr422_10_CbYCrY = 0x02200099,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed
    /// </summary>
    YCbCr422_10p = 0x02140087,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed.
    /// </summary>
    YCbCr422_10p_CbYCrY = 0x0214009A,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked.
    /// </summary>
    YCbCr422_12 = 0x02200066,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked
    /// </summary>
    YCbCr422_12_CbYCrY = 0x0220009B,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed.
    /// </summary>
    YCbCr422_12p = 0x02180088,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed.
    /// </summary>
    YCbCr422_12p_CbYCrY = 0x0218009C,
    /// <summary>
    /// YCbCr 4:4:4 8-bit BT.601.
    /// </summary>
    YCbCr601_8_CbYCr = 0x0218003D,
    /// <summary>
    /// YCbCr 4:4:4 10-bit unpacked BT.601.
    /// </summary>
    YCbCr601_10_CbYCr = 0x02300089,
    /// <summary>
    /// YCbCr 4:4:4 10-bit packed BT.601.
    /// </summary>
    YCbCr601_10p_CbYCr = 0x021E008A,
    /// <summary>
    /// YCbCr 4:4:4 12-bit unpacked BT.601.
    /// </summary>
    YCbCr601_12_CbYCr = 0x0230008B,
    /// <summary>
    /// YCbCr 4:4:4 12-bit packed BT.601.
    /// </summary>
    YCbCr601_12p_CbYCr = 0x0224008C,
    /// <summary>
    /// YCbCr 4:1:1 8-bit BT.601.
    /// </summary>
    YCbCr601_411_8_CbYYCrYY = 0x020C003F,
    /// <summary>
    /// YCbCr 4:2:2 8-bit BT.601.
    /// </summary>
    YCbCr601_422_8 = 0x0210003E,
    /// <summary>
    /// YCbCr 4:2:2 8-bit BT.601.
    /// </summary>
    YCbCr601_422_8_CbYCrY = 0x02100044,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked BT.601.
    /// </summary>
    YCbCr601_422_10 = 0x0220008D,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked BT.601.
    /// </summary>
    YCbCr601_422_10_CbYCrY = 0x0220009D,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed BT.601.
    /// </summary>
    YCbCr601_422_10p = 0x0214008E,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed BT.601.
    /// </summary>
    YCbCr601_422_10p_CbYCrY = 0x0214009E,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked BT.601.
    /// </summary>
    YCbCr601_422_12 = 0x0220008F,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked BT.601.
    /// </summary>
    YCbCr601_422_12_CbYCrY = 0x0220009F,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed BT.601.
    /// </summary>
    YCbCr601_422_12p = 0x02180090,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed BT.601.
    /// </summary>
    YCbCr601_422_12p_CbYCrY = 0x021800A0,
    /// <summary>
    /// YCbCr 4:4:4 8-bit BT.709.
    /// </summary>
    YCbCr709_8_CbYCr = 0x02180040,
    /// <summary>
    /// YCbCr 4:4:4 10-bit unpacked BT.709.
    /// </summary>
    YCbCr709_10_CbYCr = 0x02300091,
    /// <summary>
    /// YCbCr 4:4:4 10-bit packed BT.709.
    /// </summary>
    YCbCr709_10p_CbYCr = 0x021E0092,
    /// <summary>
    /// YCbCr 4:4:4 12-bit unpacked BT.709.
    /// </summary>
    YCbCr709_12_CbYCr = 0x02300093,
    /// <summary>
    /// YCbCr 4:4:4 12-bit packed BT.709.
    /// </summary>
    YCbCr709_12p_CbYCr = 0x02240094,
    /// <summary>
    /// YCbCr 4:1:1 8-bit BT.709.
    /// </summary>
    YCbCr709_411_8_CbYYCrYY = 0x020C0042,
    /// <summary>
    /// YCbCr 4:2:2 8-bit BT.709.
    /// </summary>
    YCbCr709_422_8 = 0x02100041,
    /// <summary>
    /// YCbCr 4:2:2 8-bit BT.709.
    /// </summary>
    YCbCr709_422_8_CbYCrY = 0x02100045,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked BT.709.
    /// </summary>
    YCbCr709_422_10 = 0x02200095,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked BT.709.
    /// </summary>
    YCbCr709_422_10_CbYCrY = 0x022000A1,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed BT.709.
    /// </summary>
    YCbCr709_422_10p = 0x02140096,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed BT.709.
    /// </summary>
    YCbCr709_422_10p_CbYCrY = 0x021400A2,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked BT.709.
    /// </summary>
    YCbCr709_422_12 = 0x02200097,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked BT.709.
    /// </summary>
    YCbCr709_422_12_CbYCrY = 0x022000A3,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed BT.709.
    /// </summary>
    YCbCr709_422_12p = 0x02180098,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed BT.709.
    /// </summary>
    YCbCr709_422_12p_CbYCrY = 0x021800A4,
    /// <summary>
    /// YCbCr 4:4:4 8-bit BT.2020.
    /// </summary>
    YCbCr2020_8_CbYCr = 0x021800F4,
    /// <summary>
    /// YCbCr 4:4:4 10-bit unpacked BT.2020.
    /// </summary>
    YCbCr2020_10_CbYCr = 0x023000F5,
    /// <summary>
    /// YCbCr 4:4:4 10-bit packed BT.2020.
    /// </summary>
    YCbCr2020_10p_CbYCr = 0x021E00F6,
    /// <summary>
    /// YCbCr 4:4:4 12-bit unpacked BT.2020.
    /// </summary>
    YCbCr2020_12_CbYCr = 0x023000F7,
    /// <summary>
    /// YCbCr 4:4:4 12-bit packed BT.2020.
    /// </summary>
    YCbCr2020_12p_CbYCr = 0x022400F8,
    /// <summary>
    /// YCbCr 4:1:1 8-bit BT.2020.
    /// </summary>
    YCbCr2020_411_8_CbYYCrYY = 0x020C00F9,
    /// <summary>
    /// YCbCr 4:2:2 8-bit BT.2020.
    /// </summary>
    YCbCr2020_422_8 = 0x021000FA,
    /// <summary>
    /// YCbCr 4:2:2 8-bit BT.2020.
    /// </summary>
    YCbCr2020_422_8_CbYCrY = 0x021000FB,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked BT.2020.
    /// </summary>
    YCbCr2020_422_10 = 0x022000FC,
    /// <summary>
    /// YCbCr 4:2:2 10-bit unpacked BT.2020.
    /// </summary>
    YCbCr2020_422_10_CbYCrY = 0x022000FD,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed BT.2020.
    /// </summary>
    YCbCr2020_422_10p = 0x021400FE,
    /// <summary>
    /// YCbCr 4:2:2 10-bit packed BT.2020.
    /// </summary>
    YCbCr2020_422_10p_CbYCrY = 0x021400FF,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked BT.2020.
    /// </summary>
    YCbCr2020_422_12 = 0x02200100,
    /// <summary>
    /// YCbCr 4:2:2 12-bit unpacked BT.2020.
    /// </summary>
    YCbCr2020_422_12_CbYCrY = 0x02200101,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed BT.2020.
    /// </summary>
    YCbCr2020_422_12p = 0x02180102,
    /// <summary>
    /// YCbCr 4:2:2 12-bit packed BT.2020.
    /// </summary>
    YCbCr2020_422_12p_CbYCrY = 0x02180103,
    /// <summary>
    /// YUV 4:4:4 8-bit.
    /// </summary>
    YUV8_UYV = 0x02180020,
    /// <summary>
    /// YUV 4:1:1 8-bit.
    /// </summary>
    YUV411_8_UYYVYY = 0x020C001E,
    /// <summary>
    /// YUV 4:2:2 8-bit.
    /// </summary>
    YUV422_8 = 0x02100032,
    /// <summary>
    /// YUV 4:2:2 8-bit.
    /// </summary>
    YUV422_8_UYVY = 0x0210001F,
    /// <summary>
    /// GigE Vision specific format, Monochrome 10-bit packed.
    /// </summary>
    Mono10Packed = 0x010C0004,
    /// <summary>
    /// GigE Vision specific format, Monochrome 12-bit packed.
    /// </summary>
    Mono12Packed = 0x010C0006,
    /// <summary>
    /// GigE Vision specific format, Bayer Blue-Green 10-bit packed.
    /// </summary>
    BayerBG10Packed = 0x010C0029,
    /// <summary>
    /// GigE Vision specific format, Bayer Blue-Green 12-bit packed.
    /// </summary>
    BayerBG12Packed = 0x010C002D,
    /// <summary>
    /// GigE Vision specific format, Bayer Green-Blue 10-bit packed.
    /// </summary>
    BayerGB10Packed = 0x010C0028,
    /// <summary>
    /// GigE Vision specific format, Bayer Green-Blue 12-bit packed.
    /// </summary>
    BayerGB12Packed = 0x010C002C,
    /// <summary>
    /// GigE Vision specific format, Bayer Green-Red 10-bit packed.
    /// </summary>
    BayerGR10Packed = 0x010C0026,
    /// <summary>
    /// GigE Vision specific format, Bayer Green-Red 12-bit packed.
    /// </summary>
    BayerGR12Packed = 0x010C002A,
    /// <summary>
    /// GigE Vision specific format, Bayer Red-Green 10-bit packed.
    /// </summary>
    BayerRG10Packed = 0x010C0027,
    /// <summary>
    /// GigE Vision specific format, Bayer Red-Green 12-bit packed.
    /// </summary>
    BayerRG12Packed = 0x010C002B,
    /// <summary>
    /// GigE Vision specific format, Red-Green-Blue 10-bit packed - variant 1.
    /// </summary>
    RGB10V1Packed = 0x0220001C,
    /// <summary>
    /// GigE Vision specific format, Red-Green-Blue 12-bit packed - variant 1.
    /// </summary>
    RGB12V1Packed = 0x02240034,
    /// <summary>
    /// Represents an invalid pixel format.
    /// </summary>
    InvalidPixelFormat = 0
}
#endregion

/// <summary>
/// Parameter type.
/// </summary>
public enum GcParameterType
{
    /// <summary>
    /// Integer type.
    /// </summary>
    Integer = 0,
    /// <summary>
    /// Float type.
    /// </summary>
    Float,
    /// <summary>
    /// Strign type.
    /// </summary>
    String,
    /// <summary>
    /// Enum type.
    /// </summary>
    Enumeration,
    /// <summary>
    /// Boolean type.
    /// </summary>
    Boolean,
    /// <summary>
    /// Command type.
    /// </summary>
    Command
}

/// <summary>
/// Parameter visibility.
/// </summary>
public enum GcVisibility
{
    /// <summary>
    /// Features that should be visible for all users via the GUI and API (default).
    /// </summary>
    Beginner = 0,
    /// <summary>
    /// Features that require a more in-depth knowledge of the camera functionality.
    /// </summary>
    Expert,
    /// <summary>
    /// Advanced features that might bring the cameras into a state where it will not work properly anymore if it is set incorrectly for the cameras current mode of operation.
    /// </summary>
    Guru,
    /// <summary>
    /// Features that should be kept hidden for the GUI users but still be available via the API.
    /// </summary>
    Invisible
}
