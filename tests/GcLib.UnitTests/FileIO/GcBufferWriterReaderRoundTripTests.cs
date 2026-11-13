using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GcLib.Utilities.FileIO;
using GcLib.Utilities.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Utilities;

/// <summary>
/// Tests of buffers written by <see cref="GcBufferWriter"/> and read back by <see cref="GcBufferReader"/>.
/// </summary>
[TestClass]
[DoNotParallelize]
public class GcBufferWriterReaderRoundTripTests
{
    #region Fields

    private string _path;

    #endregion

    #region Properties

    public TestContext TestContext { get; set; }

    #endregion

    #region TestConfiguration

    [TestInitialize]
    public void TestInitialize()
    {
        _path = Path.Combine(Path.GetTempPath(), $"GcBufferRoundTrip_{Guid.NewGuid():N}.bin");
    }

    [TestCleanup]
    public void TestCleanup()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    #endregion

    #region Tests

    [TestMethod]
    [DataRow(PixelFormat.Mono8, 7u, 5u)]
    [DataRow(PixelFormat.Mono10, 4u, 3u)]
    [DataRow(PixelFormat.Mono12p, 5u, 5u)] // Packed, 37.5 bytes rounded up to 38.
    [DataRow(PixelFormat.Mono16, 6u, 2u)]
    [DataRow(PixelFormat.RGB8, 3u, 4u)]
    [DataRow(PixelFormat.BGR8, 1u, 1u)]
    [DataRow(PixelFormat.Mono8, 1u, 1u)]
    public async Task WriteAndRead_PixelFormats_BuffersAreIdentical(PixelFormat pixelFormat, uint width, uint height)
    {
        // Arrange
        var written = Enumerable.Range(0, 5).Select(i => CreateBuffer(i, width, height, pixelFormat)).ToList();

        // Act
        await WriteAsync(written);

        // Assert
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);

        Assert.AreEqual((ulong)written.Count, reader.FrameCount);
        Assert.AreEqual((uint)GetImageDataSize(width, height, pixelFormat), reader.PayloadSize);
        Assert.IsFalse(reader.IsTruncated);

        for (int i = 0; i < written.Count; i++)
        {
            Assert.IsTrue(reader.ReadImage(out GcBuffer actual));
            AssertBuffersAreIdentical(written[i], actual);
            Assert.AreEqual(written[i].TimeStamp, reader.GetTimeStamp((ulong)i));
        }

        Assert.IsFalse(reader.ReadImage(out _));
    }

    [TestMethod]
    public async Task WriteAndRead_PaddedBuffers_StoresCompactPixels()
    {
        var mono = new GcBuffer([1, 2, 90, 3, 4, 91, 92], 2, 2, PixelFormat.Mono8, 1, 100, 1, 1);
        var mono2 = new GcBuffer([5, 6, 93, 7, 8, 94, 95], 2, 2, PixelFormat.Mono8, 2, 200, 1, 1);
        await WriteAsync([mono, mono2]);
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);
        Assert.AreEqual(2ul, reader.FrameCount);
        Assert.IsTrue(reader.ReadImage(out var first));
        Assert.IsTrue(reader.ReadImage(out var second));
        Assert.AreSequenceEqual(new byte[] { 1, 2, 3, 4 }, first.ImageData);
        Assert.AreSequenceEqual(new byte[] { 5, 6, 7, 8 }, second.ImageData);
        Assert.AreEqual(0u, first.PaddingX);
        Assert.AreEqual(0u, first.PaddingY);
    }

    [TestMethod]
    public async Task WriteAndRead_PackedPaddedRows_StoresContinuousBits()
    {
        var buffer = new GcBuffer([1, 0, 90, 2, 0, 91, 92], 1, 2, PixelFormat.Mono10p, 1, 100, 1, 1);
        await WriteAsync([buffer]);
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);
        Assert.IsTrue(reader.ReadImage(out var actual));
        Assert.AreSequenceEqual(new byte[] { 1, 8, 0 }, actual.ImageData);
        Assert.AreEqual(2d, actual.GetPixel(1, 0, 0));
    }

    [TestMethod]
    public async Task WriteAndRead_PackedRowsBeyondIntBitOffset_StoresCorrectPixels()
    {
        const int stride = 134217730;
        byte[] data = new byte[stride * 3];
        data[0] = 0x55;
        data[1] = 0x01;
        data[stride] = 0xAA;
        data[stride + 1] = 0x02;
        data[stride * 2] = 0xFF;
        data[stride * 2 + 1] = 0x03;
        var buffer = new GcBuffer(data, 1, 3, PixelFormat.Mono10p, 1, 100, stride - 2, 0);

        Assert.AreEqual(1023d, buffer.GetPixel(2, 0, 0));
        buffer.SetPixel(2, 0, 0, 3);
        Assert.AreSequenceEqual(new byte[] { 0x55, 0x01, 0xAA, 0x02, 0x03, 0x00 }, buffer.Unpack().ImageData);
        await WriteAsync([buffer]);
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);
        Assert.IsTrue(reader.ReadImage(out var actual));
        Assert.AreEqual(341d, actual.GetPixel(0, 0, 0));
        Assert.AreEqual(682d, actual.GetPixel(1, 0, 0));
        Assert.AreEqual(3d, actual.GetPixel(2, 0, 0));
    }

    [TestMethod]
    public async Task WriteAndRead_ManyBuffers_OrderIsPreserved()
    {
        // Arrange
        int numBuffers = 250;
        var written = Enumerable.Range(0, numBuffers).Select(i => CreateBuffer(i, 4, 4, PixelFormat.Mono8)).ToList();

        // Act
        await WriteAsync(written);

        // Assert
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);
        Assert.AreEqual((ulong)numBuffers, reader.FrameCount);

        for (int i = 0; i < numBuffers; i++)
        {
            Assert.IsTrue(reader.ReadImage(out GcBuffer actual, (ulong)i));
            Assert.AreEqual((long)i, actual.FrameID);
        }
    }

    [TestMethod]
    public async Task WriteAndRead_BuffersFromConcurrentProducers_AllBuffersAreWritten()
    {
        // Arrange
        int numProducers = 4;
        int buffersPerProducer = 50;
        using var writer = new GcBufferWriter(_path);
        writer.Start();

        // Act
        await Task.WhenAll(Enumerable.Range(0, numProducers).Select(producer => Task.Run(() =>
        {
            for (int i = 0; i < buffersPerProducer; i++)
                writer.OnBufferTransferred(this, new BufferTransferredEventArgs(CreateBuffer(producer * buffersPerProducer + i, 3, 3, PixelFormat.Mono8)));
        }, TestContext.CancellationToken)));
        await writer.StopAsync();
        writer.Dispose();

        // Assert
        Assert.AreEqual(numProducers * buffersPerProducer, writer.BuffersWritten);

        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);
        var frameIds = new List<long>();
        while (reader.ReadImage(out GcBuffer buffer))
            frameIds.Add(buffer.FrameID);

        Assert.AreSequenceEqual(Enumerable.Range(0, numProducers * buffersPerProducer).Select(i => (long)i).ToList(), frameIds, Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
    }

    [TestMethod]
    public async Task WriteAndRead_RestartedWriter_BothRunsAreReadable()
    {
        // Arrange
        using (var writer = new GcBufferWriter(_path))
        {
            writer.Start();
            writer.OnBufferTransferred(this, new BufferTransferredEventArgs(CreateBuffer(0, 3, 3, PixelFormat.Mono8)));
            await writer.StopAsync();

            // Act
            writer.Start();
            writer.OnBufferTransferred(this, new BufferTransferredEventArgs(CreateBuffer(1, 3, 3, PixelFormat.Mono8)));
            await writer.StopAsync();
        }

        // Assert
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);
        Assert.AreEqual<ulong>(2, reader.FrameCount);
        Assert.IsTrue(reader.ReadImage(out GcBuffer first, 0));
        Assert.IsTrue(reader.ReadImage(out GcBuffer second, 1));
        Assert.AreEqual(0, first.FrameID);
        Assert.AreEqual(1, second.FrameID);
    }

    [TestMethod]
    public async Task WriteAndRead_SequentialAndIndexedReads_ReturnSameBuffers()
    {
        // Arrange
        await WriteAsync([.. Enumerable.Range(0, 8).Select(i => CreateBuffer(i, 5, 3, PixelFormat.Mono16))]);
        using var reader = new GcBufferReader(_path);
        await reader.OpenAsync(token: TestContext.CancellationToken);

        // Act/Assert
        for (ulong i = 0; i < reader.FrameCount; i++)
        {
            Assert.IsTrue(reader.ReadImage(out GcBuffer indexed, i));
            reader.FrameIndex = i;
            Assert.IsTrue(reader.ReadImage(out GcBuffer sequential));
            AssertBuffersAreIdentical(indexed, sequential);
        }
    }

    [TestMethod]
    public async Task GcBufferReader_FileWithMaximumDimensions_ThrowsIOException()
    {
        // Arrange (a frame of 65535x65535 pixels would exceed 2 GB, which the reader does not support).
        byte[] data = new byte[GcBufferWriter.ImageHeaderSize + 10];
        BitConverter.GetBytes(65535u).CopyTo(data, 16);
        BitConverter.GetBytes(65535u).CopyTo(data, 20);
        BitConverter.GetBytes((int)PixelFormat.Mono8).CopyTo(data, 24);
        await File.WriteAllBytesAsync(_path, data, TestContext.CancellationToken);

        // Act/Assert
        Assert.Throws<IOException>(() => new GcBufferReader(_path));

        using var file = new FileStream(_path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    #endregion

    #region Private methods

    private async Task WriteAsync(List<GcBuffer> buffers)
    {
        using var writer = new GcBufferWriter(_path);
        writer.Start();
        foreach (var buffer in buffers)
            writer.OnBufferTransferred(this, new BufferTransferredEventArgs(buffer));
        await writer.StopAsync();

        Assert.AreEqual(buffers.Count, writer.BuffersWritten);
    }

    private static long GetImageDataSize(uint width, uint height, PixelFormat pixelFormat)
    {
        return ((long)width * height * GenICamHelper.GetBitsPerPixel(pixelFormat) + 7) / 8;
    }

    private static GcBuffer CreateBuffer(int frameId, uint width, uint height, PixelFormat pixelFormat)
    {
        // Fill with deterministic pseudo-random data, so that every frame (and every byte) differs.
        byte[] data = new byte[GetImageDataSize(width, height, pixelFormat)];
        new Random(frameId + 1).NextBytes(data);

        return new GcBuffer(data, width, height, pixelFormat, frameId, (ulong)(TimeSpan.TicksPerDay + frameId * 333333L));
    }

    private static void AssertBuffersAreIdentical(GcBuffer expected, GcBuffer actual)
    {
        Assert.AreEqual(expected.FrameID, actual.FrameID);
        Assert.AreEqual(expected.TimeStamp, actual.TimeStamp);
        Assert.AreEqual(expected.Width, actual.Width);
        Assert.AreEqual(expected.Height, actual.Height);
        Assert.AreEqual(expected.PixelFormat, actual.PixelFormat);
        Assert.AreEqual(expected.PixelDynamicRangeMax, actual.PixelDynamicRangeMax);
        Assert.IsTrue(expected.ImageData.AsSpan(0, (int)GetImageDataSize(expected.Width, expected.Height, expected.PixelFormat)).SequenceEqual(actual.ImageData), "Image data differs.");
    }

    #endregion
}
