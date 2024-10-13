namespace FusionViewer.Utilities.Imaging;

/// <summary>
/// Color converter class, containing methods for converting between different color representations in the Windows system.
/// </summary>
internal static class WindowsColorConverter
{
    /// <summary>
    /// Converts color from <see cref="System.Windows.Media"/> to <see cref="System.Drawing"/> namespaces.
    /// </summary>
    /// <param name="color">Color as System.Windows.Media.Color.</param>
    /// <returns>Color as System.Drawing.Color.</returns>
    public static System.Drawing.Color MediaToDrawing(System.Windows.Media.Color color)
    {
        return System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
    }

    /// <summary>
    /// Converts color from <see cref="System.Drawing"/> to <see cref="System.Windows.Media"/> namespaces.
    /// </summary>
    /// <param name="color">Color as System.Drawing.Color.</param>
    /// <returns>Color as System.Windows.Media.Color.</returns>
    public static System.Windows.Media.Color DrawingToMedia(System.Drawing.Color color)
    {
        return System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}