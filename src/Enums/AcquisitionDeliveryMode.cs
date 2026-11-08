namespace GcLib;

/// <summary>
/// Defines how transferred buffers are delivered from datastream to consumers.
/// </summary>
public enum AcquisitionDeliveryMode
{
    /// <summary>
    /// Buffers are enqueued to an internal output queue and retrieved by consumers via <see cref="GcDataStream.RetrieveBuffer(out GcBuffer, int)"/>.
    /// </summary>
    Synchronous,

    /// <summary>
    /// Buffers are delivered to consumers asynchronously via <see cref="GcDataStream.BufferTransferred"/> events dispatched by a non-blocking dispatcher.
    /// </summary>
    Asynchronous,
}
