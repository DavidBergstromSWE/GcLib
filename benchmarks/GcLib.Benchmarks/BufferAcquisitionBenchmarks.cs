using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using GcLib.Utilities.Imaging;

namespace GcLib.Benchmarks;

/// <summary>
/// Benchmarks frame acquisition and consumption using <see cref="GcBuffer"/> and <see cref="VideoFrame"/>.
/// </summary>
/// <remarks>Measures the two bounded-channel processing chains across configured image widths and pixel formats,
/// with memory diagnostics. A checksum validates that both chains process equivalent frame data.</remarks>
[MemoryDiagnoser]
public class BufferAcquisitionBenchmarks
{
    /// <summary>
    /// The number of frames per invocation.
    /// </summary>
    private const int FramesPerInvocation = 64;

    /// <summary>
    /// Maximum number of items the queue can hold.
    /// </summary>
    private const int QueueCapacity = 4;

    /// <summary>
    /// Pointer to the unmanaged source buffer containing the image data.
    /// </summary>
    private IntPtr _source;

    /// <summary>
    /// The number of bytes in the image.
    /// </summary>
    private int _imageBytes;

    /// <summary>
    /// Image height.
    /// </summary>
    private uint _height;

    /// <summary>
    /// The expected checksum value.
    /// </summary>
    private long _expectedChecksum;

    /// <summary>
    /// Array pool used to rent and return byte arrays.
    /// </summary>
    private ArrayPool<byte> _pool = null!;

    /// <summary>
    /// Creates a <see cref="GcBuffer"/> with a frame ID.
    /// </summary>
    private Func<long, GcBuffer> _createGcBuffer = null!;

    /// <summary>
    /// Creates a <see cref="VideoFrame"/> with a frame ID.
    /// </summary>
    private Func<long, VideoFrame> _createVideoFrame = null!;

    /// <summary>
    /// Image width.
    /// </summary>
    [Params(640, 1920)]
    public uint Width { get; set; }

    /// <summary>
    /// Pixel format of image.
    /// </summary>
    [Params(PixelFormat.Mono8, PixelFormat.RGB8)]
    public PixelFormat Format { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // Create a source buffer with a known pattern to validate the checksum.
        _height = Width == 640 ? 480u : 1080u;
        _imageBytes = checked((int)(Width * _height * GenICamHelper.GetBitsPerPixel(Format) / 8));
        byte[] source = new byte[_imageBytes];
        for (int i = 0; i < source.Length; i++)
            source[i] = (byte)(i % 251); // Use a prime number to avoid simple patterns.

        // Allocate unmanaged memory and copy the source data into it. This simulates a native memory source for the benchmarks.
        _source = Marshal.AllocHGlobal(_imageBytes);
        Marshal.Copy(source, 0, _source, _imageBytes);

        // Create an array pool for renting buffers. The pool is sized to accommodate the image bytes and a few extra buffers for the queue capacity.
        _pool = ArrayPool<byte>.Create(_imageBytes, QueueCapacity + 2);

        // Initialize the creation functions for GcBuffer and VideoFrame.
        _createGcBuffer = CreateGcBuffer;
        _createVideoFrame = CreateVideoFrame;

        // Compute the expected checksum by sampling the source data for each frame ID. This ensures that both processing chains produce equivalent results.
        for (int frameId = 0; frameId < FramesPerInvocation; frameId++)
            _expectedChecksum += Sample(source, frameId);

        // Validate both chains and warm the pool outside the measured workload.
        GcBufferChain().GetAwaiter().GetResult();
        VideoFrameChain().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        // Free the unmanaged memory allocated for the source buffer to prevent memory leaks.
        Marshal.FreeHGlobal(_source);
        _source = IntPtr.Zero;
    }

    /// <summary>
    /// Benchmarks processing a chain of multiple <see cref="GcBuffer"/> instances.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation and contains the resulting sample.</returns>
    [Benchmark(Baseline = true, OperationsPerInvoke = FramesPerInvocation)]
    public Task<long> GcBufferChain()
    {
        return RunChainAsync(create: _createGcBuffer, consume: static buffer => Sample(buffer.ImageData, buffer.FrameID), release: null);
    }
    
    /// <summary>
    /// Benchmarks processing a chain of multiple <see cref="VideoFrame"/> instances.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation and contains the resulting sample.</returns>
    [Benchmark(OperationsPerInvoke = FramesPerInvocation)]
    public Task<long> VideoFrameChain()
    {
        return RunChainAsync(create: _createVideoFrame, consume: static frame => Sample(frame.ImageData.Span, frame.FrameID), release: static frame => frame.Release());
    }

    /// <summary>
    /// Creates a <see cref="GcBuffer"/> from a copy of the current image data.
    /// </summary>
    /// <param name="frameId">The identifier of the frame associated with the buffer.</param>
    /// <returns>A <see cref="GcBuffer"/> containing the copied image data and frame metadata.</returns>
    private GcBuffer CreateGcBuffer(long frameId)
    {
        // Copy the image data from unmanaged memory into a managed byte array. This simulates acquiring frame data from a native source.
        byte[] data = new byte[_imageBytes];
        Marshal.Copy(_source, data, 0, data.Length);
        return new GcBuffer(data, Width, _height, Format, frameId, (ulong)frameId);
    }

    /// <summary>
    /// Creates a <see cref="VideoFrame"/> and copies data from native memory.
    /// </summary>
    /// <param name="frameId">The identifier of the video frame.</param>
    /// <returns>The created <see cref="VideoFrame"/>.</returns>
    private VideoFrame CreateVideoFrame(long frameId)
    {
        // Create a new VideoFrame instance with the specified dimensions, pixel format, frame ID, timestamp, and array pool. The initial reference count is set to 1.
        var frame = new VideoFrame(Width, _height, Format, frameId, (ulong)frameId, _pool);
        try
        {
            frame.CopyFromNativeMemory(_source);
            return frame;
        }
        catch
        {
            frame.Release();
            throw;
        }
    }

    /// <summary>
    /// Creates and consumes a bounded chain of frames asynchronously, then verifies the resulting checksum.
    /// </summary>
    /// <remarks>Frames are created and consumed concurrently through a bounded channel.</remarks>
    /// <typeparam name="T">The frame type passed between the creation and consumption delegates.</typeparam>
    /// <param name="create">Creates a frame for the specified frame identifier.</param>
    /// <param name="consume">Consumes a frame and returns its checksum contribution.</param>
    /// <param name="release">Releases a frame after consumption or if it cannot be transferred; may be null.</param>
    /// <returns>The verified checksum of all consumed frames.</returns>
    /// <exception cref="InvalidOperationException">The checksum of the consumed frames does not match the expected checksum.</exception>
    private async Task<long> RunChainAsync<T>(Func<long, T> create, Func<T, long> consume, Action<T>? release)
    {
        // Create a bounded channel with the specified capacity and options. The channel is used to transfer frames between the producer and consumer tasks.
        var channel = Channel.CreateBounded<T>(new BoundedChannelOptions(QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = true,
            AllowSynchronousContinuations = false
        });

        // Start the producer task that creates frames and writes them to the channel. If a frame cannot be transferred, it is released immediately.
        Task producer = Task.Run(async () =>
        {
            try
            {
                // Create and transfer the specified number of frames to the channel. Each frame is created with a unique frame ID.
                for (int frameId = 0; frameId < FramesPerInvocation; frameId++)
                {
                    T frame = create(frameId);
                    bool transferred = false;
                    try
                    {
                        // Attempt to write the frame to the channel asynchronously. If the channel is full, this will wait until space is available.
                        await channel.Writer.WriteAsync(frame).ConfigureAwait(false);
                        transferred = true;
                    }
                    finally
                    {
                        // If the frame was not successfully transferred to the channel, release it to avoid memory leaks. The release action may be null, in which case no action is taken.
                        if (!transferred)
                            release?.Invoke(frame);
                    }
                }
            }
            finally
            {
                // Complete the channel writer to signal that no more frames will be written. This allows the consumer to finish processing and exit gracefully.
                channel.Writer.TryComplete();
            }
        });

        // Start the consumer task that reads frames from the channel, consumes them, and releases them after processing. The checksum of all consumed frames is computed and returned.
        Task<long> consumer = ConsumeAsync();
        await Task.WhenAll(producer, consumer).ConfigureAwait(false);
        long checksum = await consumer.ConfigureAwait(false);

        // Verify that the computed checksum matches the expected checksum. If they do not match, an exception is thrown to indicate a data integrity issue.
        if (checksum != _expectedChecksum)
            throw new InvalidOperationException($"Acquisition checksum mismatch: expected {_expectedChecksum}, got {checksum}.");
        return checksum;

        // Consumes frames from the channel asynchronously, computes their checksum, and releases them after processing. The channel is completed when all frames have been consumed.
        async Task<long> ConsumeAsync()
        {
            long result = 0;
            try
            {
                // Read all frames from the channel asynchronously. The channel reader will yield frames as they become available, and the loop will continue until the channel is completed.
                await foreach (T frame in channel.Reader.ReadAllAsync().ConfigureAwait(false))
                {
                    try
                    {
                        // Consume the frame and accumulate its checksum contribution. The consume delegate is expected to return a long value representing the checksum of the frame.
                        result += consume(frame);
                    }
                    finally
                    {
                        // Release the frame after processing to avoid memory leaks. The release action may be null, in which case no action is taken.
                        release?.Invoke(frame);
                    }
                }
                return result; // Return the accumulated checksum after all frames have been consumed.
            }
            finally
            {
                // Ensure that the channel is completed and any remaining frames are released. This is important to avoid resource leaks in case of exceptions or early termination.
                channel.Writer.TryComplete();
                while (channel.Reader.TryRead(out T? remaining))
                    release?.Invoke(remaining);
            }
        }
    }

    /// <summary>
    /// Calculates a value from the frame identifier, data length, and selected bytes.
    /// </summary>
    /// <param name="data">A non-empty span of bytes.</param>
    /// <param name="frameId">The frame identifier to include in the calculation.</param>
    /// <returns>The sum of the frame identifier, data length, and the first, middle, and last bytes.</returns>
    private static long Sample(ReadOnlySpan<byte> data, long frameId)
    {
        return frameId + data.Length + data[0] + data[data.Length / 2] + data[^1];
    }
}
