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
    /// Size of the expected buffer data in bytes.
    /// </summary>
    uint PayloadSize { get; }

    /// <summary>
    /// Size (number of buffers) of producer input buffer pool.
    /// </summary>
    uint BufferCapacity { get; }

    /// <summary>
    /// ID of the datastream currently owning acquisition on this producer.
    /// </summary>
    string ActiveDataStreamID { get; }

    /// <summary>
    /// True if concurrent datastream acquisition is supported.
    /// </summary>
    bool SupportsConcurrentStreams { get; }

    /// <summary>
    /// Maximum number of datastreams that can acquire concurrently.
    /// </summary>
    uint MaxConcurrentStreams { get; }

    /// <summary>
    /// Start acquiring buffers.
    /// </summary>
    void StartAcquisition();

    /// <summary>
    /// Stop acquiring buffers.
    /// </summary>
    void StopAcquisition();

    /// <summary>
    /// Rents a managed image buffer from the specified datastream input buffer pool.
    /// </summary>
    /// <param name="dataStreamID">ID of datastream to rent buffer from.</param>
    /// <param name="minimumLength">Minimum required byte length of rented buffer.</param>
    /// <returns>Rented image buffer.</returns>
    byte[] RentInputBuffer(string dataStreamID, int minimumLength);

    /// <summary>
    /// Returns a managed image buffer to the specified datastream input buffer pool.
    /// </summary>
    /// <param name="dataStreamID">ID of datastream to return buffer to.</param>
    /// <param name="buffer">Managed image buffer.</param>
    void ReturnInputBuffer(string dataStreamID, byte[] buffer);

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
