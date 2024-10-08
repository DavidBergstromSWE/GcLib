using System;

namespace GcLib;

/// <summary>
/// Interface for a producer of buffers, enabling acquisition of buffers from a buffer source.
/// </summary>
public interface IBufferProducer
{
    /// <summary>
    /// Current buffer acquisition status.
    /// </summary>
    public bool IsAcquiring { get; }

    /// <summary>
    /// Start acquiring buffers.
    /// </summary>
    void StartAcquisition();

    /// <summary>
    /// Stop acquiring buffers.
    /// </summary>
    void StopAcquisition();

    /// <summary>
    /// Event raised when acquisition is started.
    /// </summary>
    event EventHandler AcquisitionStarted;

    /// <summary>
    /// Event raised when acquisition is stopped.
    /// </summary>
    event EventHandler AcquisitionStopped;

    /// <summary>
    /// Event raised when acquisition failed and is aborted, containing an error message in the event arguments.
    /// </summary>
    event EventHandler<AcquisitionAbortedEventArgs> AcquisitionAborted;

    /// <summary>
    /// Event raised when a new buffer has been successfully acquired, containing the buffer in the event arguments.
    /// </summary>
    event EventHandler<NewBufferEventArgs> NewBuffer;

    /// <summary>
    /// Event raised when the acquisition of a buffer failed.
    /// </summary>
    event EventHandler FailedBuffer;
}