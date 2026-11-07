namespace GcLib;

/// <summary>
/// Coordinates acquisition and release of datastreams.
/// </summary>
internal interface IStreamCoordinator
{
    /// <summary>
    /// Starts acquisition of specified datastream.
    /// </summary>
    /// <param name="dataStreamID">Identifier of datastream.</param>
    void StartStream(string dataStreamID);

    /// <summary>
    /// Stops acquisition of specified datastream.
    /// </summary>
    /// <param name="dataStreamID">Identifier of datastream.</param>
    void StopStream(string dataStreamID);
}
