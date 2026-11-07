namespace GcLib;

/// <summary>
/// Coordinates the acquisition and release of data streams.
/// </summary>
internal interface IStreamAcquisitionCoordinator
{
    /// <summary>
    /// Starts acquisition of the specified data stream.
    /// </summary>
    /// <param name="dataStreamID">The identifier of the data stream to acquire.</param>
    void StartStreamAcquisition(string dataStreamID);

    /// <summary>
    /// Stops acquisition of the specified data stream.
    /// </summary>
    /// <param name="dataStreamID">The identifier of the data stream to stop acquiring.</param>
    void StopStreamAcquisition(string dataStreamID);
}
