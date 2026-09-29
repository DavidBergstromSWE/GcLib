namespace GcLib;

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
