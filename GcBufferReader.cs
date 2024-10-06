using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GcLib.Utilities.Imaging;

namespace GcLib;

/// <summary>
/// Reader of image buffers from file.
/// </summary>
public sealed class GcBufferReader : IDisposable
{
    #region Fields

    /// <summary>
    /// Provides a stream for a file, supporting both synchronous and asynchronous read and write operations.
    /// </summary>
    private readonly FileStream _fileStream;

    /// <summary>
    /// Size of file header (bytes).
    /// </summary>
    private readonly uint _fileHeaderSize = GcBufferWriter.FileHeaderSize;

    /// <summary>
    /// Size of image header (bytes).
    /// </summary>
    private readonly uint _imageHeaderSize = GcBufferWriter.ImageHeaderSize;

    /// <summary>
    /// Size of image buffer data (bytes).
    /// </summary>
    private readonly uint _imageBufferSize;

    /// <summary>
    /// Size of the expected image (including image header and buffer data) in bytes.
    /// </summary> 
    private readonly uint _imageSize;

    /// <summary>
    /// List of image buffer IDs.
    /// </summary>
    private readonly List<long> _frameIDList;

    /// <summary>
    /// List of image buffer timestamps.
    /// </summary>
    private readonly List<ulong> _timeStampList;

    // backing fields
    private ulong _frameIndex = 0;

    /// <summary>
    /// True if object has been disposed.
    /// </summary>
    private bool _disposed;

    #endregion

    #region Properties

    /// <summary>
    /// Size of the expected buffer data in bytes.
    /// </summary>
    public uint PayloadSize => _imageBufferSize;

    /// <summary>
    /// Total size of file in bytes.
    /// </summary>
    public ulong FileSize { get; private set; }

    /// <summary>
    /// A relative or absolute path to the opened file.
    /// </summary>
    public string FilePath { get; private set; }

    /// <summary>
    /// Zero-based index of current frame in file.
    /// </summary>
    public ulong FrameIndex
    {
        get => _frameIndex = _disposed ? 0 : (ulong)(_fileStream.Position - _fileHeaderSize) / _imageSize;

        set
        {
            if (value < FrameCount)
            {
                _frameIndex = value;
                SetFilePosition(_frameIndex);
            }
            else
            {
                throw new IndexOutOfRangeException($"Frame index out of range! {FilePath} only contain {FrameCount} image(s)!");
            }
        }
    }

    /// <summary>
    /// Number of image frames in file.
    /// </summary>
    public ulong FrameCount { get; private set; }

    /// <summary>
    /// Calculated frame rate (in Hz) of image sequence in file.
    /// </summary>
    public double FrameRate { get; private set; }

    /// <summary>
    /// True if reader has been disposed (and is unuseable). Instantiate new object to read new data.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// True if reader is opened and ready for reading buffers.
    /// </summary>
    public bool IsOpen { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Instantiates a new reader of buffers from specified file. The reader needs to be opened using <see cref="OpenAsync(IProgress{double}, CancellationToken)"/> to start accessing the buffers.
    /// </summary>
    /// <param name="filePath">A relative or absolute path to the file.</param>
    /// <exception cref="FileNotFoundException"></exception>
    /// <exception cref="IOException"/>
    public GcBufferReader(string filePath)
    {
        // Try to open file.
        _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
        
        FilePath = filePath;

        // Retrieve file size.
        FileSize = (ulong)_fileStream.Length;

        // Read file header.
        if (_fileHeaderSize > _fileStream.Length)
            throw new IOException("Could not read file header!");
        byte[] fileHeader = new byte[_fileHeaderSize];
        _ = _fileStream.Read(fileHeader, 0, fileHeader.Length);

        // Read image header of first frame.
        if (_imageHeaderSize > _fileStream.Length - _fileStream.Position)
            throw new IOException("Could not read image header!");
        byte[] imageHeader = new byte[_imageHeaderSize];
        _ = _fileStream.Read(imageHeader, 0, imageHeader.Length);

        // Retrieve expected buffer and image sizes.
        _imageBufferSize = GetWidth(imageHeader) * GetHeight(imageHeader) * GenICamConverter.GetBitsPerPixel(GetPixelFormat(imageHeader)) / 8;
        _imageSize = _imageBufferSize + _imageHeaderSize;

        if ((_fileStream.Length - _fileHeaderSize) % _imageSize != 0)
            throw new IOException("File corrupt?");
        else FrameCount = (ulong)((_fileStream.Length - _fileHeaderSize) / _imageSize);

        _frameIDList = [];
        _timeStampList = [];
    }

    #endregion

    #region IDisposable

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged and (optionally) managed resources.
    /// </summary>
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
    /// <c>false</c> to release only unmanaged resources, called from the finalizer only.</param>
    private void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            // Dispose managed state (managed objects).                
            _frameIDList.Clear();
            _timeStampList.Clear();

            IsOpen = false;
        }

        // Free unmanaged resources (unmanaged objects) (and set large fields to null).

        // Close and dispose filestream.
        _fileStream?.Close();
        _fileStream?.Dispose();

        _disposed = true;
    }

    /// <summary>
    /// Finalizer.
    /// </summary>
    ~GcBufferReader()
    {
        Dispose(false);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Opens reader, after which is ready for accessing buffers.
    /// </summary>
    /// <param name="progress">Provides an optional progress report of the task.</param>
    /// <param name="token">Provides an optional way of canceling the task.</param>
    /// <returns></returns>
    /// <exception cref="IOException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    public async Task OpenAsync(IProgress<double> progress = null, CancellationToken token = default)
    {
        // Initialize lists of image FrameID and Timestamp.
        await ReadAllBuffersAsync(progress, token);

        // Validate frame count.
        if (FrameCount == 0)
            throw new IOException("File does not contain any frames!");
        
        if (FrameCount != (ulong)_frameIDList.Count)
            throw new IOException("File corrupt?");

        // Estimate frame rate.
        FrameRate = CalculateFrameRate(_timeStampList);

        // Reset position of file to first frame.
        FrameIndex = 0;

        IsOpen = true;
    }

    /// <summary>
    /// Returns timestamp of image with specified frame index.
    /// </summary>
    /// <param name="frameIndex">Zero-based frame index.</param>
    /// <returns>Timestamp (in PC ticks).</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public ulong GetTimeStamp(ulong frameIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsOpen == false)
            throw new InvalidOperationException("The reader needs to be opened before accessing it!");

        return frameIndex <= FrameCount - 1
            ? _timeStampList[(int)frameIndex]
            : throw new IndexOutOfRangeException($"The zero-based frame index is out of range! File contains {FrameCount} images.");
    }

    /// <summary>
    /// Read image from file with specified frame index.
    /// </summary>
    /// <param name="buffer">Buffer read or null if operation was unsuccessful.</param>
    /// <param name="frameIndex">Zero-based frame index.</param>
    /// <returns>True if grab was successful.</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public bool ReadImage(out GcBuffer buffer, ulong frameIndex)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsOpen == false)
            throw new InvalidOperationException("The reader needs to be opened before accessing it!");

        // Initialize output.
        buffer = null;

        // Try to set file position to frame index.
        try
        {
            SetFilePosition(frameIndex);
        }
        catch (IndexOutOfRangeException)
        {
            // Return false at exception.
            return false;
        }

        // Read image buffer data.
        byte[] imageData = new byte[_imageSize];
        _ = _fileStream.Read(imageData, 0, imageData.Length);

        // Convert to GcBuffer.
        buffer = ToGcBuffer(imageData);

        return buffer != null;
    }

    /// <summary>
    /// Read next image from file.
    /// </summary>
    /// <param name="buffer">Buffer read or null if operation was unsuccessful.</param>
    /// <returns>True if grab was successful.</returns>
    /// <exception cref="ObjectDisposedException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public bool ReadImage(out GcBuffer buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsOpen == false)
            throw new InvalidOperationException("The reader needs to be opened before accessing it!");

        // Initialize output.
        buffer = null;

        // Check end-of-file.
        if (_imageSize > _fileStream.Length - _fileStream.Position)
            return false;

        // Read next image buffer data.
        byte[] imageData = new byte[_imageSize];
        _ = _fileStream.Read(imageData, 0, imageData.Length);

        // Convert to GcBuffer.
        buffer = ToGcBuffer(imageData);

        return true;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Returns frame ID of image from image header.
    /// </summary>
    /// <param name="imageHeader">Image header data.</param>
    /// <returns>Frame ID.</returns>
    private static long GetFrameID(Span<byte> imageHeader)
    {
        return BitConverter.ToInt64(imageHeader[..sizeof(long)]);
    }

    /// <summary>
    /// Returns timestamp of image from image header.
    /// </summary>
    /// <param name="imageHeader">Image header data.</param>
    /// <returns>Timestamp.</returns>
    private static ulong GetTimeStamp(Span<byte> imageHeader)
    {
        return BitConverter.ToUInt64(imageHeader.Slice(sizeof(ulong), sizeof(ulong)));
    }

    /// <summary>
    /// Returns width of image from image header.
    /// </summary>
    /// <param name="imageHeader">Image header data.</param>
    /// <returns>Width (number of pixels).</returns>
    private static uint GetWidth(Span<byte> imageHeader)
    {
        return BitConverter.ToUInt32(imageHeader.Slice(sizeof(ulong) + sizeof(ulong), sizeof(uint)));
    }

    /// <summary>
    /// Returns height of image from image header.
    /// </summary>
    /// <param name="imageHeader">Image header data.</param>
    /// <returns>Height (number of pixels).</returns>
    private static uint GetHeight(Span<byte> imageHeader)
    {
        return BitConverter.ToUInt32(imageHeader.Slice(sizeof(ulong) + sizeof(ulong) + sizeof(int), sizeof(uint)));
    }

    /// <summary>
    /// Returns pixel format of image from image header.
    /// </summary>
    /// <param name="imageHeader">Image header data.</param>
    /// <returns>Pixel format.</returns>
    private static PixelFormat GetPixelFormat(Span<byte> imageHeader)
    {
        return (PixelFormat)BitConverter.ToInt32(imageHeader.Slice(sizeof(ulong) + sizeof(ulong) + sizeof(int) + sizeof(int), sizeof(int)));
    }

    /// <summary>
    /// Returns maximum possible (saturation) pixel value from image header.
    /// </summary>
    /// <param name="imageHeader">Image header data.</param>
    /// <returns>Pixel dynamic range maximum.</returns>
    private static uint GetDynamicRangeMax(Span<byte> imageHeader)
    {
        return BitConverter.ToUInt32(imageHeader.Slice(sizeof(ulong) + sizeof(ulong) + sizeof(int) + sizeof(int) + sizeof(int), sizeof(uint)));
    }

    /// <summary>
    /// Reads all buffers in the file and initializes lists of buffer metadata contained.
    /// </summary>
    /// <param name="progress">Provider of progress updates.</param>
    /// <param name="token">Token for notifying cancellations.</param>
    /// <returns>(awaitable) <see cref="Task"/>.</returns>
    /// <exception cref="IOException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    private async Task ReadAllBuffersAsync(IProgress<double> progress, CancellationToken token = default)
    {
        // Set position of filestream after fileheader.
        _fileStream.Seek(_fileHeaderSize, SeekOrigin.Begin);

        // Allocate space for image and image header.
        var imagesBytes = new byte[_imageHeaderSize + _imageBufferSize];
        byte[] imageHeaderBytes = new byte[_imageHeaderSize];

        // Initialize buffer counter.
        int bufferCounter = 0;

        // Step through filestream until end-of-file.
        while (await _fileStream.ReadAsync(imagesBytes, CancellationToken.None) == (int)_imageSize)
        {
            // Simulates a slow reading (comment out in production code!).
            //Thread.Sleep(30);

            // Read image header.
            Buffer.BlockCopy(imagesBytes, 0, imageHeaderBytes, 0, imageHeaderBytes.Length);

            // Extract frame ID and timestamp from header.
            _frameIDList.Add(GetFrameID(imageHeaderBytes));
            _timeStampList.Add(GetTimeStamp(imageHeaderBytes));

            token.ThrowIfCancellationRequested();

            // Report progress using throttling.
            if (bufferCounter++ == 10)
            {
                progress?.Report((double)_fileStream.Position / _fileStream.Length);
                bufferCounter = 0;
            }
        }

        // Throw exception if EOF is not reached.
        if (_fileStream.Position != _fileStream.Length)
        {
            throw new IOException("File is corrupt?");
        }
        else progress?.Report(1.0);
    }

    /// <summary>
    /// Returns a calculated frame rate (in frames per second), estimated from a list of timestamps.
    /// </summary>
    /// <param name="timeStamps">List of timestamps.</param>
    /// <returns>Estimated frame rate (or 0 if sequence only contains one image or if timestamps are equal).</returns>
    private static double CalculateFrameRate(List<ulong> timeStamps)
    {
        var timeSpan = TimeSpan.FromTicks((long)timeStamps[^1] - (long)timeStamps[0]);
        return timeSpan.TotalSeconds > 0 ? timeStamps.Count / timeSpan.TotalSeconds : 0;
    }

    /// <summary>
    /// Converts image buffer data to <see cref="GcBuffer"/> object.
    /// </summary>
    /// <param name="buffer">Image buffer.</param>
    /// <returns><see cref="GcBuffer"/> object with image and chunkdata.</returns>
    private GcBuffer ToGcBuffer(byte[] buffer)
    {
        // Retrieve image header.
        Span<byte> imageHeader = new(buffer, 0, (int)_imageHeaderSize);

        // Read chunk data from header.
        long frameID = GetFrameID(imageHeader);
        ulong timeStamp = GetTimeStamp(imageHeader);
        uint width = GetWidth(imageHeader);
        uint height = GetHeight(imageHeader);
        PixelFormat pixelFormat = GetPixelFormat(imageHeader);
        uint pixelDynamicRangeMax = GetDynamicRangeMax(imageHeader);

        // Copy image buffer data.
        byte[] imageData = new byte[_imageBufferSize];
        Buffer.BlockCopy(buffer, imageHeader.Length, imageData, 0, imageData.Length);

        // Convert to new GcBuffer object.
        return new GcBuffer(imageData: imageData,
                            width: width,
                            height: height,
                            pixelFormat: pixelFormat,
                            pixelDynamicRangeMax: pixelDynamicRangeMax,
                            frameID: frameID,
                            timeStamp: timeStamp);
    }

    /// <summary>
    /// Gets file position of specific frame index.
    /// </summary>
    /// <param name="frameIndex">Zero-based frame index.</param>
    /// <returns>File position in bytes.</returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    private long GetFilePosition(ulong frameIndex)
    {
        return frameIndex < FrameCount
            ? (long)(_fileHeaderSize + (frameIndex * _imageSize))
            : throw new IndexOutOfRangeException($"Frame index {frameIndex} is out of range (must be less than {FrameCount})!");
    }

    /// <summary>
    /// Sets file position to specific frame index.
    /// </summary>
    /// <param name="frameIndex">Zero-based frame index.</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    private void SetFilePosition(ulong frameIndex)
    {
        _ = frameIndex < FrameCount
            ? _fileStream.Seek(GetFilePosition(frameIndex), SeekOrigin.Begin)
            : throw new IndexOutOfRangeException($"Frame index {frameIndex} is out of range (must be less than {FrameCount})!");
    }

    #endregion
}