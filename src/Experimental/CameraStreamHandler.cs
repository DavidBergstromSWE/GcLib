namespace GcLib;

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Coordinates GUI and AI processing of video frames through separate consumer queues.
/// </summary>
/// <remarks>Frames are shared between the consumers, and their pooled buffers are released when both consumers
/// finish or decline them.</remarks>
public class MultiConsumerCameraHandler
{
    /// <summary>
    /// Thread-safe queue of video frames pending GUI processing.
    /// </summary>
    /// <remarks>Supports producer-consumer coordination and blocks when taking items until frames are
    /// available or adding is completed.</remarks>
    private readonly BlockingCollection<VideoFrame> _guiQueue = [];

    /// <summary>
    /// Thread-safe queue of video frames pending AI processing.
    /// </summary>
    /// <remarks>Supports concurrent producers and consumers for frame processing workflows.</remarks>
    private readonly BlockingCollection<VideoFrame> _aiQueue = [];

    /// <summary>
    /// Provides access to the shared pool of byte arrays for buffer reuse.
    /// </summary>
    private readonly ArrayPool<byte> _bufferPool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Signals cancellation for operations managed by the containing type.
    /// </summary>
    /// <remarks>Dispose this instance when the containing type is disposed to release unmanaged
    /// resources.</remarks>
    private readonly CancellationTokenSource _cts = new();

    /// <summary>
    /// Starts the GUI and AI consumer loops on separate background tasks.
    /// </summary>
    /// <remarks>Returns immediately after scheduling both loops with the shared cancellation token.</remarks>
    public void Start()
    {
        Task.Run(() => GuiConsumerLoop(_cts.Token));
        Task.Run(() => AiConsumerLoop(_cts.Token));
    }

    /// <summary>
    /// Creates a video frame from unmanaged image data and dispatches it to the GUI and AI processing queues.
    /// </summary>
    /// <remarks>Initializes the frame for two consumers. If either queue cannot accept the frame, that
    /// consumer's reference is released immediately so the frame can be returned to the pool when no longer in
    /// use.</remarks>
    /// <param name="rawImagePointer">Pointer to unmanaged memory that contains the source pixel buffer for the frame.</param>
    /// <param name="width">Frame width, in pixels.</param>
    /// <param name="height">Frame height, in pixels.</param>
    /// <param name="pixelFormat">Pixel format of the source frame data.</param>
    /// <param name="frameID">Identifier of the frame.</param>
    /// <param name="timeStamp">Timestamp associated with the frame.</param>
    public void OnFrameReceived(IntPtr rawImagePointer, uint width, uint height, PixelFormat pixelFormat, long frameID, ulong timeStamp)
    {
        const int numberOfConsumers = 2; // GUI & AI

        // Create frame and copy raw source data
        var frame = new VideoFrame(width, height, pixelFormat, frameID, timeStamp, _bufferPool, numberOfConsumers);
        frame.CopyFromNativeMemory(rawImagePointer);

        // Send 
        if (!_guiQueue.TryAdd(frame))
        {
            // GUI hann inte med. Minska räknaren direkt eftersom denna konsument hoppar över bildrutan.
            frame.Release();
        }

        // 3. Skicka till AI-kön (AI kanske har en striktare kö där vi inte vill droppa frames)
        if (!_aiQueue.TryAdd(frame))
        {
            frame.Release();
        }
    }

    // KONSUMENT 1: GUI-Tråden (Här vill vi ofta droppa frames om det laggar)
    private void GuiConsumerLoop(CancellationToken token)
    {
        foreach (var frame in _guiQueue.GetConsumingEnumerable(token))
        {
            try
            {
                // Uppdatera GUI (t.ex. rita ut frame.Data på skärmen)
                RenderInGui(frame.ImageData.Span);
            }
            finally
            {
                // VIKTIGT: Berätta att GUI är klar. Om AI också är klar, returneras minnet till poolen här.
                frame.Release();
            }
        }
    }

    // KONSUMENT 2: AI/Analys-Tråden
    private void AiConsumerLoop(CancellationToken token)
    {
        foreach (var frame in _aiQueue.GetConsumingEnumerable(token))
        {
            try
            {
                // Kör tung analys (t.ex. objektdetektering)
                RunAiInference(frame.ImageData.Span);
            }
            finally
            {
                // VIKTIGT: Berätta att AI är klar.
                frame.Release();
            }
        }
    }

    private static void RenderInGui(ReadOnlySpan<byte> _) { /* ... */ }
    private static void RunAiInference(ReadOnlySpan<byte> _) { /* ... */ }
}
