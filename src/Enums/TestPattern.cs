namespace GcLib;

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
