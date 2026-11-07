using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GcLib.Utilities.Collections;

namespace GcLib;

/// <summary>
/// Represents the device level in the GenICam/GenTL standard module hierarchy, responsible for device communication and instantiation of datastreams.
/// </summary>
/// <remarks>All API-derived device classes need to inherit from this abstract base class through <see cref="GcDevice{TDevice}"/>.</remarks> 
public abstract partial class GcDevice : IBufferProducer, IStreamCoordinator, IEnumerable<GcDataStream>
{
    #region Fields

    /// <summary>
    /// Dictionary containing the available datastreams on the device, where key is datastream ID and value is the datastream.
    /// </summary>
    protected Dictionary<string, GcDataStream> _dataStreamList;

    /// <summary>
    /// Synchronization lock for datastream collection access.
    /// </summary>
    private readonly Lock _dataStreamListLock = new();

    /// <summary>
    /// Synchronizes access to acquisition operations.
    /// </summary>
    private readonly Lock _acquisitionLock = new();

    /// <summary>
    /// Tracks datastreams with active acquisitions.
    /// </summary>
    private readonly HashSet<string> _activeDataStreamAcquisitions = [];

    /// <summary>
    /// Registered stream-bound sinks for buffer and failure delivery, where key is datastream ID and value is a tuple containing the new-buffer and failed-buffer callbacks.
    /// </summary>
    private readonly Dictionary<string, (Action<object, NewBufferEventArgs> OnNewBuffer, Action<object, EventArgs> OnFailedBuffer)> _bufferSinks = [];

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
    public virtual uint BufferCapacity { get; set; } = 4;

    /// <summary>
    /// Acquisition status of device.
    /// </summary>
    public bool IsAcquiring { get; protected set; }

    /// <summary>
    /// Maximum number of datastreams that can acquire concurrently. A value of one means concurrent acquisition is not supported.
    /// </summary>
    public virtual uint MaxConcurrentStreams => 1;

    #endregion

    #region Constructors

    /// <summary>
    /// Base class constructor, initializes a new empty list of datastreams on the device.
    /// </summary>
    private protected GcDevice()
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
        DispatchNewBuffer(eventArgs);
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
        DispatchFailedBuffer();
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

    /// <summary>
    /// When overridden in a derived class, performs post-close operations for the device.
    /// </summary>
    /// <remarks>Implementations should release device-specific resources and update closure-related
    /// state.</remarks>
    protected abstract void OnDeviceClosed();

    #endregion

    #region Public Methods

    /// <summary>
    /// Opens a new or existing datastream on the device.
    /// </summary>
    /// <param name="dataStreamID">(Optional) Unique ID of the datastream.</param>
    /// <param name="deliveryMode">(Optional) Delivery mode for the datastream.</param>
    /// <returns>Opened datastream.</returns>
    public GcDataStream OpenDataStream(string dataStreamID = null, AcquisitionDeliveryMode deliveryMode = AcquisitionDeliveryMode.Synchronous)
    {
        lock (_dataStreamListLock)
        {
            // If ID is empty then create a unique one using Guid.
            if (string.IsNullOrEmpty(dataStreamID))
                dataStreamID = Guid.NewGuid().ToString();
            else
                // else return datastream with ID if available in the list of datastreams.
                if (_dataStreamList.TryGetValue(dataStreamID, out GcDataStream value))
                    return value;

            // Create new datastream on the device, using unique ID.
            var dataStream = new GcDataStream(bufferProducer: this, dataStreamID: dataStreamID, outputQueueCapacity: 4, deliveryMode: deliveryMode);

            // Add new datastream to list of available datastreams.
            _dataStreamList.Add(dataStreamID, dataStream);

            return dataStream;
        }
    }

    /// <summary>
    /// Close device by shutting down all opened datastreams.
    /// </summary>
    public void Close()
    {
        // Snapshot open datastreams outside of close loop lock.
        GcDataStream[] dataStreams;
        lock (_dataStreamListLock)
            dataStreams = [.. _dataStreamList.Values];

        // Stop acquisition and close streams before releasing device-specific resources.
        foreach (var dataStream in dataStreams)
        {
            if (dataStream.IsOpen)
                dataStream.Close();
        }

        // Perform device-specific closure operations after streams have stopped.
        OnDeviceClosed();

        // Clear list of datastreams.
        lock (_dataStreamListLock)
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
        lock (_dataStreamListLock)
        {
            return index < _dataStreamList.Count
                ? _dataStreamList.ElementAt((int)index).Key
                : throw new ArgumentOutOfRangeException($"Index {index} is out of range! Only {_dataStreamList.Count} datastreams are available.");
        }
    }

    /// <summary>
    /// Queries the number of available datastreams on the device.
    /// </summary>
    /// <returns>Number of datastreams.</returns>
    public uint GetNumDataStreams()
    {
        lock (_dataStreamListLock)
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
        // Open a temporary datastream for fetching a single image, using queued delivery mode.
        var dataStream = OpenDataStream(deliveryMode: AcquisitionDeliveryMode.Synchronous);
        dataStream.Start(1);
        var result = dataStream.RetrieveImage(out buffer, (int)timeout);
        dataStream.Stop();
        dataStream.Close();

        return result;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Dispatches a new buffer to registered sink(s).
    /// </summary>
    /// <param name="eventArgs">Event arguments containing stream ID and payload metadata.</param>
    private void DispatchNewBuffer(NewBufferEventArgs eventArgs)
    {
        // Determine which sinks to dispatch to based on the stream ID in the event arguments.
        Action<object, NewBufferEventArgs>[] sinks;

        lock (_dataStreamListLock)
        {
            // If a stream ID is provided, dispatch only to the sink registered for that stream ID (if any).
            if (string.IsNullOrEmpty(eventArgs.StreamID) == false)
            {
                if (_bufferSinks.TryGetValue(eventArgs.StreamID, out var sink))
                    sinks = [sink.OnNewBuffer]; // Dispatch to the sink registered for the provided stream ID.
                else sinks = []; // Dispatch to no sinks if no sink is registered for the provided stream ID.
            }
            else sinks = [.. _bufferSinks.Values.Select(x => x.OnNewBuffer)]; // If no stream ID is provided, dispatch to all registered sinks.
        }

        // Invoke the new-buffer callback for each sink.
        foreach (var sink in sinks)
            sink(this, eventArgs);
    }

    /// <summary>
    /// Dispatches failed-buffer notification to all registered sinks.
    /// </summary>
    private void DispatchFailedBuffer()
    {
        Action<object, EventArgs>[] sinks;

        lock (_dataStreamListLock)
            sinks = [.. _bufferSinks.Values.Select(x => x.OnFailedBuffer)]; // Get all registered failed-buffer callbacks from the dictionary.

        // Invoke the failed-buffer callback for each sink.
        foreach (var sink in sinks)
            sink(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes a previously registered stream sink.
    /// </summary>
    /// <param name="dataStreamID">Datastream ID.</param>
    private void UnregisterBufferSink(string dataStreamID)
    {
        if (string.IsNullOrEmpty(dataStreamID))
            return;

        lock (_dataStreamListLock)
            _bufferSinks.Remove(dataStreamID); // Remove the sink associated with the provided stream ID from the dictionary.
    }

    #endregion

    #region IBufferProducer

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">The data stream is already being acquired, the maximum number of concurrent data streams has been reached, or the configured maximum is zero.</exception>
    void IStreamCoordinator.StartStream(string dataStreamID)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataStreamID);

        lock (_acquisitionLock)
        {
            // Check if datastream is already acquiring.
            if (_activeDataStreamAcquisitions.Contains(dataStreamID))
                throw new InvalidOperationException($"Datastream '{dataStreamID}' is already acquiring.");

            // Check if maximum number of concurrent streams has been reached.
            uint maxConcurrentStreams = MaxConcurrentStreams;
            if (maxConcurrentStreams == 0)
                throw new InvalidOperationException("MaxConcurrentStreams must be greater than zero.");
            if ((uint)_activeDataStreamAcquisitions.Count >= maxConcurrentStreams)
                throw new InvalidOperationException($"The device supports a maximum of {maxConcurrentStreams} concurrent datastream(s).");

            // Add datastream ID to set of active acquisitions.
            _activeDataStreamAcquisitions.Add(dataStreamID);

            // If first datastream, start device acquisition.
            if (_activeDataStreamAcquisitions.Count == 1)
            {
                try
                {
                    StartAcquisition(); // Start device acquisition.
                }
                catch
                {
                    _activeDataStreamAcquisitions.Remove(dataStreamID); // Roll back if starting acquisition fails.
                    throw; // Rethrow exception to propagate error to caller.
                }
            }
        }
    }

    void IStreamCoordinator.StopStream(string dataStreamID)
    {
        lock (_acquisitionLock)
        {
            // Check if the datastream is currently acquiring.
            if (_activeDataStreamAcquisitions.Contains(dataStreamID) == false)
                return;

            // If there are multiple active datastream acquisitions, just remove the specified datastream ID from the set and return.
            if (_activeDataStreamAcquisitions.Count > 1)
            {
                _activeDataStreamAcquisitions.Remove(dataStreamID);
                return;
            }

            // If this is the last active datastream acquisition, stop device acquisition and remove the datastream ID from the set.
            StopAcquisition();
            _activeDataStreamAcquisitions.Remove(dataStreamID);
        }
    }

    /// <summary>
    /// Registers stream-bound callbacks for buffer delivery and failed-buffer notifications.
    /// </summary>
    /// <param name="dataStreamID">Unique datastream ID.</param>
    /// <param name="onNewBuffer">Callback for new-buffer delivery for this stream.</param>
    /// <param name="onFailedBuffer">Callback for failed-buffer notifications for this stream.</param>
    /// <returns>Disposable registration handle.</returns>
    IDisposable IBufferProducer.RegisterBufferSink(string dataStreamID, Action<object, NewBufferEventArgs> onNewBuffer, Action<object, EventArgs> onFailedBuffer)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataStreamID);
        ArgumentNullException.ThrowIfNull(onNewBuffer);
        ArgumentNullException.ThrowIfNull(onFailedBuffer);

        lock (_dataStreamListLock)
            _bufferSinks[dataStreamID] = (onNewBuffer, onFailedBuffer); // Register the provided callbacks for the specified datastream ID in the dictionary.

        return new SinkRegistration(this, dataStreamID); // Return disposable registration handle for stream-bound sink subscription.
    }

    #endregion

    #region Private Classes

    /// <summary>
    /// Disposable registration handle for stream-bound sink subscriptions.
    /// </summary>
    /// <param name="device">Device instance.</param>
    /// <param name="dataStreamID">Datastream ID.</param>
    private sealed class SinkRegistration(GcDevice device, string dataStreamID) : IDisposable
    {
        /// <summary>
        /// Stores the GcDevice instance associated with this object.
        /// </summary>
        private GcDevice _device = device;

        /// <summary>
        /// Identifier for the datastream.
        /// </summary>
        private readonly string _dataStreamID = dataStreamID;

        /// <summary>
        /// Releases resources by unregistering the buffer sink from the current device.
        /// </summary>
        /// <remarks>Thread-safe and idempotent; subsequent calls have no effect after the buffer sink has
        /// been unregistered.</remarks>
        public void Dispose()
        {
            Interlocked.Exchange(ref _device, null)?.UnregisterBufferSink(_dataStreamID);
        }
    }

    #endregion

    #region Protected Methods (GenApi-related)

    /// <summary>
    /// Imports parameters and features implemented by device.
    /// </summary>
    /// <returns>Collection of device parameters.</returns>
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
        lock (_dataStreamListLock)
            return _dataStreamList.Values.ToList().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion
}