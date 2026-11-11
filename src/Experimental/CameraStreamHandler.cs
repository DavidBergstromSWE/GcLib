using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace GcLib;

/// <summary>
/// Processes camera frames asynchronously using pooled buffers.
/// </summary>
public class CameraStreamHandler
{
    /// <summary>
    /// The collection of video frames awaiting processing.
    /// </summary>
    private readonly BlockingCollection<VideoFrame> _frameQueue = new(boundedCapacity: 100); // Alternatively, use Channel<VideoFrame> for more advanced scenarios.
    private readonly object _lifecycleLock = new();
    private Task _consumerTask;
    private bool _isStopped;

    /// <summary>
    /// The pool of byte arrays used to minimize allocations.
    /// </summary>
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Starts the background loop that processes images.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if processing has already started or the handler has stopped.</exception>
    public void StartProcessingLoop()
    {
        lock (_lifecycleLock)
        {
            if (_isStopped || _consumerTask != null)
                throw new InvalidOperationException("The processing loop has already started or the handler has stopped.");

            _consumerTask = Task.Run(ConsumerLoop);
        }
    }

    /// <summary>
    /// Copies a received camera frame into a pooled buffer and attempts to enqueue it for processing.
    /// </summary>
    /// <remarks>If the queue is full, the frame is discarded and disposed. Exceptions are caught and written
    /// to the console.</remarks>
    /// <param name="rawImagePointer">Pointer to the native memory containing the frame data.</param>
    /// <param name="width">Frame width in pixels.</param>
    /// <param name="height">Frame height in pixels.</param>
    /// <param name="pixelFormat">Pixel format of the frame.</param>
    /// <param name="frameID">Identifier of the frame.</param>
    /// <param name="timestamp">Timestamp of the frame.</param>
    public void OnFrameReceived(IntPtr rawImagePointer, uint width, uint height, PixelFormat pixelFormat, long frameID, ulong timestamp)
    {
        lock (_lifecycleLock)
        {
            if (_consumerTask == null || _isStopped)
            {
                Console.WriteLine("Warning: Frame ignored because processing is not running.");
                return;
            }
        }

        VideoFrame frame = null;
        try
        {
            // Create a VideoFrame (which internally rents memory from the pool)
            frame = new VideoFrame(width, height, pixelFormat, frameID, timestamp, _bufferPool);

            // Copy the data directly from the camera's native memory to our pooled buffer.
            frame.CopyFromNativeMemory(rawImagePointer);

            // Send it to the queue without blocking the camera thread.
            if (!_frameQueue.TryAdd(frame))
            {
                frame.Dispose();
                frame = null;
                Console.WriteLine("Warning: Frame dropped! The consumer is not keeping up.");
            }
            else
            {
                frame = null;
            }
        }
        catch (Exception ex)
        {
            frame?.Dispose();
            Console.WriteLine($"Error in camera callback: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes video frames from the queue at its own pace.
    /// </summary>
    private void ConsumerLoop()
    {
        try
        {
            foreach (var frame in _frameQueue.GetConsumingEnumerable())
            {
                using (frame)
                    ProcessFrame(frame.ImageData.Span, (int)frame.Width, (int)frame.Height);
            }
        }
        finally
        {
            lock (_lifecycleLock)
            {
                if (_isStopped == false)
                {
                    _isStopped = true;
                    _frameQueue.CompleteAdding();
                }
            }

            while (_frameQueue.TryTake(out var pendingFrame))
                pendingFrame.Dispose();
        }
    }

    /// <summary>
    /// Processes a single video frame.
    /// </summary>
    /// <param name="frameData">The frame data as a read-only span of bytes.</param>
    /// <param name="width">The width of the frame in pixels.</param>
    /// <param name="height">The height of the frame in pixels.</param>
    private void ProcessFrame(ReadOnlySpan<byte> frameData, int width, int height)
    {
        // Example of super-fast processing with Span without allocations
        // e.g., check the value of the first pixel:
        byte firstPixel = frameData[0];
    }

    /// <summary>
    /// Completes the frame queue and waits for all accepted frames to be processed.
    /// </summary>
    public void Stop()
    {
        StopAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Completes the frame queue and asynchronously waits for all accepted frames to be processed.
    /// </summary>
    public Task StopAsync()
    {
        Task consumerTask;
        bool drainQueue;
        lock (_lifecycleLock)
        {
            if (_isStopped == false)
            {
                _isStopped = true;
                _frameQueue.CompleteAdding();
            }

            consumerTask = _consumerTask;
            drainQueue = consumerTask == null;
        }

        if (drainQueue)
        {
            while (_frameQueue.TryTake(out var pendingFrame))
                pendingFrame.Dispose();
        }

        return consumerTask ?? Task.CompletedTask;
    }
}
