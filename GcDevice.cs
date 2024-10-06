using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GcLib.Utilities.Collections;

namespace GcLib;

/// <summary>
/// Represents the device level in the GenICam/GenTL standard module hierarchy, responsible for device communication and instantiation of datastreams. All API-derived device classes need to inherit from this abstract base class.
/// </summary>
public abstract partial class GcDevice : IBufferProducer, IEnumerable<GcDataStream>
{
    #region Fields

    /// <summary>
    /// Dictionary containing the available datastreams on the device, where key is datastream ID and value is the datastream.
    /// </summary>
    protected Dictionary<string, GcDataStream> _dataStreamList;

    #endregion

    #region Properties

    /// <summary>
    /// Size of the expected buffer data in bytes.
    /// </summary> 
    public abstract uint PayloadSize { get; }

    /// <summary>
    /// Device top-level information.
    /// </summary>
    public GcDeviceInfo DeviceInfo { get; protected set; }

    /// <summary>
    /// Contains a cached collection of parameters implemented by the device. Update the collection (using <see cref="IReadOnlyParameterCollection.Update"/>) to get the most current values from device.
    /// </summary>
    public virtual IReadOnlyParameterCollection Parameters { get; protected set; }

    /// <summary>
    /// Size (number of buffers) of device input buffer pool. 
    /// </summary>
    public abstract uint BufferCapacity { get; set; }

    /// <summary>
    /// Acquisition status of device.
    /// </summary>
    public bool IsAcquiring { get; protected set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Base class constructor, initializes a new empty list of datastreams on the device.
    /// </summary>
    protected GcDevice()
    {
        _dataStreamList = [];
    }

    #endregion

    #region Events

    /// <summary>
    /// Event announcing that the cached parameter list is invalidated due to a recent change of a parameter, containing the changed parameter in the event arguments.
    /// </summary>
    public event EventHandler<ParameterInvalidateEventArgs> ParameterInvalidate;

    /// <summary>
    /// Event-invoking method announcing that the cached parameter list is invalidated, due to a recent parameter change where the changed parameter is in the event arguments.
    /// </summary>
    protected virtual void OnParameterInvalidate(ParameterInvalidateEventArgs eventArgs)
    {
        ParameterInvalidate?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// New buffer event announcing that a filled buffer is available for transfer, containing image data in the event arguments.
    /// </summary>
    public event EventHandler<NewBufferEventArgs> NewBuffer;

    /// <summary>
    /// Event-invoking method announcing that a filled buffer is available for transfer, containing image data in the event arguments.
    /// </summary>
    /// <param name="eventArgs">Event arguments containing image buffer.</param>
    protected virtual void OnNewBuffer(NewBufferEventArgs eventArgs)
    {
        NewBuffer?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Event raised when the acquisition of a buffer failed.
    /// </summary>
    public event EventHandler FailedBuffer;

    /// <summary>
    /// Event-invoking method announcing that the acquisition of a buffer failed.
    /// </summary>
    protected virtual void OnFailedBuffer()
    {
        FailedBuffer?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event announcing that connection to device was lost.
    /// </summary>
    public event EventHandler ConnectionLost;

    /// <summary>
    /// Event-invoking method announcing that connection to device was lost.
    /// </summary>
    protected virtual void OnConnectionLost()
    {
        ConnectionLost?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event announcing that acquisition on the device has started.
    /// </summary>
    public event EventHandler AcquisitionStarted;

    /// <summary>
    /// Event-invoking method announcing that acquisition on the device has started.
    /// </summary>
    protected virtual void OnAcquisitionStarted()
    {
        AcquisitionStarted?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event announcing that acquisition on the device has stopped
    /// </summary>
    public event EventHandler AcquisitionStopped;

    /// <summary>
    /// Event-invoking method announcing that acquisition on the device has stopped
    /// </summary>
    protected virtual void OnAcquisitionStopped()
    {
        AcquisitionStopped?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event announcing that acquisition has failed and was aborted, containing error message in the event arguments.
    /// </summary>
    public event EventHandler<AcquisitionAbortedEventArgs> AcquisitionAborted;

    /// <summary>
    /// Event-invoking method announcing that acquisition has failed and was aborted, containing error message in the event arguments.
    /// </summary>
    /// <param name="eventArgs">Event argument containing error message about acquisition failure.</param>
    protected virtual void OnAcquisitionAborted(AcquisitionAbortedEventArgs eventArgs)
    {
        AcquisitionAborted?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Event announcing that device has been disconnected and closed.
    /// </summary>
    public event EventHandler Closed;

    /// <summary>
    /// Event-invoking method announcing that device has been disconnected and closed.
    /// </summary>
    protected virtual void OnClosed()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Opens a new or existing datastream on the device.
    /// </summary>
    /// <param name="dataStreamID">(Optional) Unique ID of the datastream.</param>
    /// <returns>Opened datastream.</returns>
    public GcDataStream OpenDataStream(string dataStreamID = null)
    {
        // If ID is empty then create a unique one using Guid.
        if (string.IsNullOrEmpty(dataStreamID))
            dataStreamID = Guid.NewGuid().ToString();
        else
        // else return datastream with ID if available in the list of datastreams.
        if (_dataStreamList.TryGetValue(dataStreamID, out GcDataStream value))
            return value;

        // Create new datastream on the device, using unique ID.
        var dataStream = new GcDataStream(this, dataStreamID);

        // Add new datastream to list of available datastreams.
        _dataStreamList.Add(dataStreamID, dataStream);

        return dataStream;
    }

    /// <summary>
    /// Close device by shutting down all opened datastreams.
    /// </summary>
    public virtual void Close()
    {
        // Close all open datastreams.
        foreach (var dataStream in _dataStreamList.Values)
        {
            if (dataStream.IsOpen)
                dataStream.Close();
        }

        // Clear list of datastreams.
        _dataStreamList?.Clear();

        // Update device info.
        if (DeviceInfo != null)
        {
            DeviceInfo.IsOpen = false;
            DeviceInfo.IsAccessible = true;
        }

        // Raise event.
        OnClosed();
    }

    /// <summary>
    /// Queries the unique ID of a datastream on the device.
    /// </summary>
    /// <param name="index">Zero-based index of datastream on the device.</param>
    /// <returns>Datastream ID.</returns>
    /// <exception cref="ArgumentOutOfRangeException"/>
    public string GetDataStreamID(uint index)
    {
        return index < _dataStreamList.Count
            ? _dataStreamList.ElementAt((int)index).Key
            : throw new ArgumentOutOfRangeException($"Index {index} is out of range! Only {_dataStreamList.Count} datastreams are available.");
    }

    /// <summary>
    /// Queries the number of available datastreams on the device.
    /// </summary>
    /// <returns>Number of datastreams.</returns>
    public uint GetNumDataStreams()
    {
        return (uint)_dataStreamList.Count;
    }

    /// <summary>
    /// Starts acquisition on the device.
    /// </summary>
    public abstract void StartAcquisition();

    /// <summary>
    /// Stops acquisition on the device.
    /// </summary>
    public abstract void StopAcquisition();

    /// <summary>
    /// Fetches a single image from device, using a timeout request. To continously grab images, use <see cref="StartAcquisition"/> instead.
    /// </summary>
    /// <param name="buffer">Fetched image (or null if fetch was unsuccessful).</param>
    /// <param name="timeout">Maximum time interval to wait for the image (in milliseconds).</param>
    /// <returns><see langword="True"/> if fetch was successful, with the image in the <paramref name="buffer"/>.</returns>
    public bool FetchImage(out GcBuffer buffer, uint timeout = 1000)
    {
        GcBuffer image = null;

        // Use AutoResetEvent to wait for an image or timeout.
        using var waitHandle = new AutoResetEvent(false);

        // Define event handler.
        void OnNewBuffer(object sender, NewBufferEventArgs eventArgs) { image = eventArgs.Buffer; _ = waitHandle.Set(); }

        // Register handler to new buffer event.
        NewBuffer += OnNewBuffer;

        // Start acquisition in camera.
        StartAcquisition();

        // Wait for image or timeout.
        bool result = waitHandle.WaitOne((int)timeout);

        // Stop acquisition in camera.
        StopAcquisition();

        // Unregister handler from new buffer event.
        NewBuffer -= OnNewBuffer;

        buffer = image;

        return result;
    }

    #endregion

    #region Protected Methods (GenApi-related)

    /// <summary>
    /// Imports parameters and features implemented by device into a parameter collection.
    /// </summary>
    /// <returns>Collection of parameters.</returns>
    protected abstract GcDeviceParameterCollection ImportParameters();

    /// <summary>
    /// Get value of parameter from device as a string.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter value as string (or null if parameter is not found or of wrong type).</returns>
    protected abstract string GetParameterValue(string parameterName);

    /// <summary>
    /// Set value of parameter to device from a string.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <param name="parameterValue">New parameter value as string.</param>
    protected abstract void SetParameterValue(string parameterName, string parameterValue);

    /// <summary>
    /// Executes parameter command in device.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    protected abstract void ExecuteParameterCommand(string parameterName);

    /// <summary>
    /// Retrieves device parameter casted as a <see cref="GcBoolean"/>.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcBoolean"/> (or null if parameter is not found or of wrong type).</returns>
    protected abstract GcBoolean GetBoolean(string parameterName);

    /// <summary>
    /// Retrieves device parameter casted as a <see cref="GcCommand"/>.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcCommand"/> (or null if parameter is not found or of wrong type).</returns>
    protected abstract GcCommand GetCommand(string parameterName);

    /// <summary>
    /// Retrieves device parameter casted as a <see cref="GcEnumeration"/>.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcEnumeration"/> (or null if parameter is not found or of wrong type).</returns>
    protected abstract GcEnumeration GetEnumeration(string parameterName);

    /// <summary>
    /// Retrieves device parameter casted as a <see cref="GcFloat"/>.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcFloat"/> (or null if parameter is not found or of wrong type).</returns>
    protected abstract GcFloat GetFloat(string parameterName);

    /// <summary>
    /// Retrieves device parameter casted as a <see cref="GcInteger"/>.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcInteger"/> (or null if parameter is not found or of wrong type).</returns>
    protected abstract GcInteger GetInteger(string parameterName);

    /// <summary>
    /// Retrieves device parameter casted as a <see cref="GcString"/>.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter as <see cref="GcString"/> (or null if parameter is not found or of wrong type).</returns>
    protected abstract GcString GetString(string parameterName);

    /// <summary>
    /// Retrieves device parameter casted in its corresponding primitive data type.
    /// </summary>
    /// <param name="parameterName">Parameter name.</param>
    /// <returns>Parameter (or null if parameter is not found).</returns>
    protected virtual GcParameter GetParameter(string parameterName)
    {
        if (Parameters.IsImplemented(parameterName) == false)
            return null;

        return Parameters[parameterName].Type switch
        {
            GcParameterType.Integer => GetInteger(parameterName),
            GcParameterType.Float => GetFloat(parameterName),
            GcParameterType.String => GetString(parameterName),
            GcParameterType.Boolean => GetBoolean(parameterName),
            GcParameterType.Enumeration => GetEnumeration(parameterName),
            GcParameterType.Command => GetCommand(parameterName),
            _ => null,
        };
    }

    #endregion

    #region IEnumerable<GcDataStream>

    public IEnumerator<GcDataStream> GetEnumerator()
    {
        return _dataStreamList.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}