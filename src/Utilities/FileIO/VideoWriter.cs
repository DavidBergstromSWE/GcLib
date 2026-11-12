using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cyotek.Collections.Generic;
using GcLib.Utilities.Imaging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace GcLib.Utilities.FileIO;

/// <summary>
/// Video writer, taking buffers and compressing them into an mp4 or avi file using a specified video codec. Currently supported codecs are enumerated in <see cref="CODEC"/>.
/// <para>The class is a wrapper around <see cref="Emgu.CV.VideoWriter"/>, which is used to write input images to the output video format file.</para>
/// </summary>
public class VideoWriter : IDisposable
{
    #region Enums

    /// <summary>
    /// Supported video codecs.
    /// </summary>
    public enum CODEC
    {
        /// <summary>
        /// Motion JPEG. Intraframe-only compression in which each video frame or interlaced field of a digital video sequence is compressed separately as a JPEG image.
        /// </summary>
        MJPEG = 1196444237,
        /// <summary>
        /// x264 (open-source version of H.264/AVC video coding format).
        /// Block-oriented, motion-compensated compression using integer discrete cosine transform (DCT) with 4×4 and 8×8 block sizes.
        /// </summary>
        X264 = 875967064,
        /// <summary>
        /// H.264/AVC (Advanced Video Coding). Block-oriented, motion-compensated compression using integer discrete cosine transform (DCT) with 4×4 and 8×8 block sizes.
        /// </summary>
        //H264 = 875967048,
        /// <summary>
        /// H.265/HEVC (High Efficiency Video Coding). Block-oriented, motion-compensated compression using both integer discrete cosine transform (DCT) and discrete sine transform (DST) with varied block sizes between 4×4 and 32×32.
        /// </summary>
        //H265 = 892744264
    }

    #endregion

    #region Fields

    /// <summary>
    /// Supported pixel formats.
    /// </summary>
    public static readonly List<PixelFormat> SupportedPixelFormats =
    [
        PixelFormat.Mono8,
        PixelFormat.RGB8,
        PixelFormat.BGR8
    ];

    /// <summary>
    /// Video writer.
    /// </summary>
    private Emgu.CV.VideoWriter _videoWriter;

    /// <summary>
    /// Buffer queue, containing images waiting to be written to file.
    /// </summary>
    private readonly ConcurrentQueue<GcBuffer> _bufferQueue = new();

    /// <summary>
    /// Circular buffer of timestamps. Increase its capacity to improve accuracy of fps calculation.
    /// </summary>
    private readonly CircularBuffer<ulong> _timeStamps = new(capacity: 30, allowOverwrite: false);

    /// <summary>
    /// Recording thread used in writing to file.
    /// </summary>
    private Thread _recordingThread;

    /// <summary>
    /// Writer state (one of <see cref="StateIdle"/>, <see cref="StateStarting"/>, <see cref="StateRunning"/> or <see cref="StateStopping"/>). Transitions out of idle and running are made using <see cref="Interlocked"/> operations.
    /// </summary>
    private int _state = StateIdle;

    /// <summary>
    /// Indicates that the state is idle.
    /// </summary>
    private const int StateIdle = 0;

    /// <summary>
    /// Indicates that the state is running.
    /// </summary>
    private const int StateRunning = 1;

    /// <summary>
    /// Indicates that the writer is stopping.
    /// </summary>
    private const int StateStopping = 2;

    /// <summary>
    /// Indicates that the state is starting.
    /// </summary>
    private const int StateStarting = 3;

    /// <summary>
    /// Synchronizes access to the timestamp buffer, which is written by the producer and read by the writing thread.
    /// </summary>
    private readonly Lock _timeStampsLock = new();

    /// <summary>
    /// Serializes the transition to the stopping state, so that concurrent callers share the same stop operation.
    /// </summary>
    private readonly Lock _stopLock = new();

    /// <summary>
    /// The stop operation currently in progress.
    /// </summary>
    private Task _stopTask;

    /// <summary>
    /// Frame rate specified at construction (0 if it should be measured).
    /// </summary>
    private readonly double _configuredFps;

    /// <summary>
    /// True when the frame rate is known (specified or measured from a full timestamp buffer), allowing the recording thread to start writing.
    /// </summary>
    private volatile bool _readyToWrite;

    /// <summary>
    /// Non-zero if writing has been aborted by an error (used with <see cref="Interlocked"/> so that the error is only reported once).
    /// </summary>
    private int _aborted;

    /// <summary>
    /// Non-zero if object has been disposed (used with <see cref="Interlocked"/>).
    /// </summary>
    private int _disposed;

    /// <summary>
    /// Waithandle used for blocking recording thread while waiting for buffers to be queued.
    /// </summary>
    private readonly AutoResetEvent _waitHandle = new(false);

    #endregion

    #region Properties

    /// <summary>
    /// Number of buffers currently queued for writing.
    /// </summary>
    public int BuffersQueued => _bufferQueue.Count;

    /// <summary>
    /// Number of frames written to video file.
    /// </summary>
    public int FramesWritten { get; private set; } = 0;

    /// <summary>
    /// Flag indicating whether writing is currently active.
    /// </summary>
    public bool IsWriting => Volatile.Read(ref _state) != StateIdle;

    /// <summary>
    /// True if writer has been disposed (and is unuseable). Instantiate new object to write new data.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>
    /// A relative or absolute path to the file.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Frame rate (in frames per second).
    /// </summary>
    public double FPS { get; private set; }

    /// <summary>
    /// Video codec to use in compression.
    /// </summary>
    public CODEC Codec { get; }

    #endregion

    /// <summary>
    /// Creates a new video writer, using the specified <paramref name="filePath"/> (must be in mp4 or avi format/extension). 
    /// Videos will be saved at a frame rate specified by <paramref name="fps"/>. If no frame rate is specified, it will be calculated from buffer timestamps. 
    /// Saved video will be compressed using the scheme specified by <paramref name="codec"/>.
    /// </summary>
    /// <param name="filePath">File path to save videos to (must have mp4 or avi extension).</param>
    /// <param name="fps">Frame rate in frames per second. If not specified, it will be calculated from input buffer timestamps.</param>
    /// <param name="codec">Video codec to use in video compression.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public VideoWriter(string filePath, double fps = 0.0, CODEC codec = CODEC.MJPEG)
    {
        string extension = Path.GetExtension(filePath);
        if ((string.Equals(extension, ".mp4", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".avi", StringComparison.OrdinalIgnoreCase)) == false)
            throw new ArgumentException("Input file must have mp4 or avi extension!");

        if (fps < 0.0)
            throw new ArgumentOutOfRangeException(nameof(fps), "Frame rate cannot be negative!");

        FilePath = filePath;
        _configuredFps = fps;
        FPS = fps;
        Codec = codec;
    }

    #region Public methods

    /// <summary>
    /// Start writing video frames.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        // Atomically claim the writer (buffers are not accepted until it has been reset below).
        if (Interlocked.CompareExchange(ref _state, StateStarting, StateIdle) != StateIdle)
            throw new InvalidOperationException("Writer is already in use!");

        // Reset state from any previous run.
        _bufferQueue.Clear();
        lock (_timeStampsLock)
            _timeStamps.Clear();
        FPS = _configuredFps;
        FramesWritten = 0;
        _readyToWrite = _configuredFps > 0.0;
        Volatile.Write(ref _aborted, 0);

        // Start accepting buffers.
        Volatile.Write(ref _state, StateRunning);

        // Start recording thread (as background thread, so that it never keeps the process alive).
        _recordingThread = new Thread(ThreadProc) { IsBackground = true };
        try
        {
            _recordingThread.Start();
        }
        catch
        {
            Volatile.Write(ref _state, StateIdle); // Reset state to idle, so that writer can be restarted.
            throw;
        }
    }

    /// <summary>
    /// Stop writing video frames, writing all remaining queued buffers to file. Concurrent calls share the same stop operation. Does nothing if the writer is not writing.
    /// </summary>
    public Task StopAsync()
    {
        lock (_stopLock)
        {
            // Only one caller can stop a running writer.
            if (Interlocked.CompareExchange(ref _state, StateStopping, StateRunning) == StateRunning)
                _stopTask = StopCoreAsync();
            else if (Volatile.Read(ref _state) != StateStopping)
                return Task.CompletedTask;

            return _stopTask;
        }
    }

    private async Task StopCoreAsync()
    {
        // Stop thread immediately (without waiting for next buffer).
        _ = _waitHandle.Set();

        try
        {
            // Wait for thread to terminate and write remaining buffers to file (off the caller's thread).
            // Do not capture the caller's context, as Dispose blocks on this task and would otherwise deadlock on e.g. a UI thread.
            await Task.Run(() =>
            {
                _recordingThread?.Join();
                WriteRemainingBuffers();
            }).ConfigureAwait(false);
        }
        finally
        {
            // Close the video file, so that it is finalized and released.
            _videoWriter?.Dispose();
            _videoWriter = null;

            // Reset timestamps, so that writer can be restarted.
            lock (_timeStampsLock)
                _timeStamps.Clear();

            Volatile.Write(ref _state, StateIdle); // Reset state to idle, so that writer can be restarted.
        }
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Continuously waits for buffers to be queued and writes queued buffers to video file.
    /// </summary>
    private void ThreadProc()
    {
        while (Volatile.Read(ref _state) == StateRunning)
        {
            // Block thread until the frame rate is known and a buffer has been queued.
            if (_readyToWrite == false || _bufferQueue.IsEmpty)
            {
                _ = _waitHandle.WaitOne();
                continue;
            }

            // Retrieve buffer from queue.
            if (_bufferQueue.TryDequeue(out GcBuffer buffer))
            {
                try
                {
                    WriteBuffer(buffer);
                }
                catch (Exception ex)
                {
                    HandleWriteError(ex);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Write remaining buffers in queue to file.
    /// </summary>
    private void WriteRemainingBuffers()
    {
        // Nothing more is written once writing has been aborted.
        if (Volatile.Read(ref _aborted) != 0)
        {
            _bufferQueue.Clear();
            return;
        }

        while (_bufferQueue.TryDequeue(out var buffer))
        {
            try
            {
                WriteBuffer(buffer);
            }
            catch (Exception ex)
            {
                HandleWriteError(ex);
                break;
            }
        }
    }

    /// <summary>
    /// Aborts writing after an error (clearing the queue) and reports it once.
    /// </summary>
    private void HandleWriteError(Exception ex)
    {
        // Stop accepting buffers before clearing the queue, so that nothing is queued behind the clear.
        bool alreadyReported = Interlocked.Exchange(ref _aborted, 1) != 0;
        _bufferQueue.Clear();

        if (alreadyReported)
            return;

        // Log error.
        if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
            GcLibrary.Logger.LogError(ex, "Failed to write data to {filePath}", FilePath);

        // Raise event.
        OnWritingAborted(ex.Message, ex);
    }

    /// <summary>
    /// Write buffer to video file.
    /// </summary>
    /// <param name="buffer">Buffer to be written to file.</param>
    private void WriteBuffer(GcBuffer buffer)
    {
        if (SupportedPixelFormats.Contains(buffer.PixelFormat) == false)
            throw new NotSupportedException($"Pixel format {buffer.PixelFormat} not supported!");

        // If fps is not specified, calculate it as an average from incoming buffer timestamps.
        if (FPS == 0.0)
        {
            double elapsedTicks = 0.0;
            int intervals = 0;
            lock (_timeStampsLock)
            {
                if (_timeStamps.Size > 0)
                {
                    elapsedTicks = (double)_timeStamps.Max() - _timeStamps.Min();
                    intervals = _timeStamps.Size - 1;
                }
            }

            double measuredFPS = FrameRateCalculator.Calculate(elapsedTicks, intervals, TimeSpan.TicksPerSecond);
            FPS = measuredFPS > 0.0 ? measuredFPS : 30.0; // Use the default when timestamps cannot determine a rate.
        }

        // Initialize new video writer (if not done already), using selected codec, fps and buffer properties.
        _videoWriter ??= new(fileName: FilePath, apiPreference: 0, compressionCode: (int)Codec, fps: FPS, size: new Size((int)buffer.Width, (int)buffer.Height), isColor: GenICamHelper.GetNumChannels(buffer.PixelFormat) > 1);

        // Emgu does not throw if the file or codec cannot be opened (frames would be silently discarded).
        if (_videoWriter.IsOpened == false)
        {
            _videoWriter.Dispose();
            _videoWriter = null;
            throw new IOException($"Unable to open {FilePath} for writing using codec {Codec}!");
        }

        // Write buffer (converted to Mat).
        using var mat = buffer.ToMat();
        _videoWriter.Write(mat);

        FramesWritten++;
    }

    #endregion

    #region Events

    /// <summary>
    /// Event-handling method to BufferTransferred events, queuing transferred buffer for writing.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments, containing the transferred buffer.</param>
    public void OnBufferTransferred(object sender, BufferTransferredEventArgs e)
    {
        // Ignore buffers when not writing, while stopping (they would never be written) or after writing has been aborted.
        if (Volatile.Read(ref _state) != StateRunning || Volatile.Read(ref _aborted) != 0)
            return;

        // Add buffer timestamp to circular buffer (before queueing, so that timestamps are available when the buffer is written).
        lock (_timeStampsLock)
        {
            if (_timeStamps.IsFull == false)
                _timeStamps.Put(e.Buffer.TimeStamp);

            // Frame rate can now be measured.
            if (_timeStamps.IsFull)
                _readyToWrite = true;
        }

        // Queue transferred buffer.
        _bufferQueue.Enqueue(e.Buffer);

        // Proceed with recording thread.
        if (_readyToWrite)
            _ = _waitHandle.Set();
    }

    /// <summary>
    /// Event announcing that writing was aborted by an error (e.g., an <see cref="IOException"/> or an unsupported pixel format), with the exception in the event arguments. Raised once per recording, on the writing thread. Writing must still be stopped by calling <see cref="StopAsync"/>.
    /// </summary>
    public event EventHandler<WritingAbortedEventArgs> WritingAborted;

    /// <summary>
    /// Event-invoking method, announcing that an <see cref="IOException"/> was thrown while writing, with the exception in the event arguments.
    /// </summary>
    /// <param name="ex">Exception that was thrown.</param>
    private void OnWritingAborted(string message, Exception ex)
    {
        // Subscriber exceptions must not escape onto the recording thread.
        try
        {
            WritingAborted?.Invoke(this, new WritingAbortedEventArgs(message, ex));
        }
        catch (Exception handlerException)
        {
            if (GcLibrary.Logger.IsEnabled(LogLevel.Error))
                GcLibrary.Logger.LogError(handlerException, "WritingAborted event handler threw an exception");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(disposing: true);
    }

    /// <summary>
    /// Releases unmanaged and (optionally) managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources, called from the finalizer only.</param>
    private void Dispose(bool disposing)
    {
        if (disposing == false)
            return;

        // Only the first call disposes.
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            // Stop recording thread (waits for a stop already in progress).
            StopAsync().Wait();
        }
        finally
        {
            // Flush queues.
            _bufferQueue.Clear();
            lock (_timeStampsLock)
                _timeStamps.Clear();

            // Dispose writer.
            _videoWriter?.Dispose();
            _videoWriter = null;

            // Dispose WaitHandle.
            _waitHandle.Dispose();
        }
    }

    #endregion
}