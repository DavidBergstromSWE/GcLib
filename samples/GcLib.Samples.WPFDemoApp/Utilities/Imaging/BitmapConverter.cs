using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FusionViewer.Utilities.Imaging;

/// <summary>
/// Utility class for converting bitmaps between <see cref="System.Drawing.Imaging"/> and <see cref="System.Windows.Media.Imaging"/> namespaces.
/// </summary>
internal static partial class BitmapConverter
{
    #region Public methods

    /// <summary>
    /// Converts a <see cref="Bitmap"/> image to a <see cref="BitmapSource"/> image (4x faster than HBitmap?).
    /// </summary>
    /// <param name="bitmap">Image to convert.</param>
    /// <returns>Converted <see cref="BitmapSource"/>.</returns>
    public static BitmapSource BitmapToBitmapSourceGdi(Bitmap bitmap)
    {
        // Lock bitmap data into system memory.
        var bitmapData = bitmap.LockBits(rect: new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                                         flags: ImageLockMode.ReadOnly,
                                         format: bitmap.PixelFormat);

        // Unlock bitmap from system memory.
        bitmap.UnlockBits(bitmapData);

        // Create bitmap source and return it.
        return BitmapSource.Create(pixelWidth: bitmap.Width,
                                   pixelHeight: bitmap.Height,
                                   dpiX: bitmap.HorizontalResolution,
                                   dpiY: bitmap.VerticalResolution,
                                   pixelFormat: ConvertPixelFormat(bitmap.PixelFormat), // supported pixel format?
                                   palette: null,
                                   buffer: bitmapData.Scan0,
                                   bufferSize: bitmapData.Stride * bitmapData.Height,
                                   stride: bitmapData.Stride);
    }

    /// <summary>
    /// Converts a <see cref="Bitmap"/> image to a <see cref="BitmapSource"/> image, using unmanaged pointer.
    /// </summary>
    /// <param name="bitmap">Image to convert.</param>
    /// <returns>Converted <see cref="BitmapSource"/>.</returns>
    public static BitmapSource BitmapToBitmapSourceH(Bitmap bitmap)
    {
        nint ptr = bitmap.GetHbitmap();
        BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(bitmap: ptr,
                                                                                       palette: nint.Zero,
                                                                                       sourceRect: Int32Rect.Empty,
                                                                                       sizeOptions: BitmapSizeOptions.FromEmptyOptions());
        _ = DeleteObject(ptr);

        return bs;
    }

    /// <summary>
    /// Converts a <see cref="BitmapSource"/> image to a <see cref="Bitmap"/> image.
    /// </summary>
    /// <param name="bitmapSource">Image to convert.</param>
    /// <returns>Converted <see cref="Bitmap"/>.</returns>
    public static Bitmap BitmapSourceToBitmap(BitmapSource bitmapSource)
    {
        var bmp = new Bitmap(bitmapSource.PixelWidth, bitmapSource.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

        BitmapData data = bmp.LockBits(new Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        bitmapSource.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
        bmp.UnlockBits(data);

        return bmp;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Deletes object, freeing all system resources associated with it.
    /// </summary>
    /// <param name="obj">Handle to object.</param>
    /// <returns>Non-zero value if successful (zero if handle is invalid).</returns>
    [LibraryImport("gdi32")]
    private static partial int DeleteObject(nint obj);

    /// <summary>
    /// Converts pixelformats from <see cref="System.Drawing.Imaging"/> to <see cref="System.Windows.Media"/> namespaces.
    /// </summary>
    /// <param name="sourceFormat">Source format.</param>
    /// <returns>Destination format.</returns>
    private static System.Windows.Media.PixelFormat ConvertPixelFormat(System.Drawing.Imaging.PixelFormat sourceFormat)
    {
        return sourceFormat switch
        {
            System.Drawing.Imaging.PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
            System.Drawing.Imaging.PixelFormat.Format32bppRgb => PixelFormats.Bgr32,
            System.Drawing.Imaging.PixelFormat.Format8bppIndexed => PixelFormats.Gray8,
            System.Drawing.Imaging.PixelFormat.Format16bppGrayScale => PixelFormats.Gray16,
            _ => throw new NotSupportedException($"Pixelformat {sourceFormat} is not supported!"),
        };
    }

    #endregion
}