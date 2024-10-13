using System.Threading;
using MahApps.Metro.Controls.Dialogs;

namespace FusionViewer.Utilities.Dialogs;

/// <summary>
/// Helper class for using dialogs within the <see cref="MahApps.Metro.Controls.Dialogs"/> namespace.
/// </summary>
public static class MetroDialogHelper
{
    /// <summary>
    /// Default dialog settings for application.
    /// </summary>
    public static MetroDialogSettings DefaultSettings => new()
    {
        //AffirmativeButtonText = "OK",
        AnimateHide = true,
        AnimateShow = true,
        //CancellationToken = System.Threading.CancellationToken.None,
        ColorScheme = MetroDialogColorScheme.Theme,
        //CustomResourceDictionary = null,
        //DefaultButtonFocus = MessageDialogResult.Affirmative,
        //DefaultText = "",
        //DialogButtonFontSize = 12,
        DialogTitleFontSize = 30,
        DialogMessageFontSize = 22,
        //DialogResultOnCancel = MessageDialogResult.Canceled,
        //FirstAuxiliaryButtonText = "",
        //MaximumBodyHeight = double.NaN,
        //NegativeButtonText = "",
        //OwnerCanCloseWithDialog = true,
        //SecondAuxiliaryButtonText = "",
    };

    /// <summary>
    /// Settings for message dialogs.
    /// </summary>
    public static MetroDialogSettings MessageDialogSettings => new()
    {
        AnimateHide = false,
        AnimateShow = false,
        ColorScheme = MetroDialogColorScheme.Theme,
        DialogTitleFontSize = 26,
        DialogMessageFontSize = 20,
    };

    /// <summary>
    /// Retrieve settings for a progress dialog.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token (if needed).</param>
    /// <returns>Dialog settings.</returns>
    public static MetroDialogSettings GetProgressDialogSettings(CancellationToken? cancellationToken = null)
    {
        var settings = new MetroDialogSettings
        {
            AnimateHide = false,
            AnimateShow = false,
            ColorScheme = MetroDialogColorScheme.Theme,
            DialogTitleFontSize = 30,
            DialogMessageFontSize = 22,
            CancellationToken = cancellationToken != null ? (CancellationToken)cancellationToken : CancellationToken.None,
            DialogResultOnCancel = MessageDialogResult.Canceled,
        };

        return settings;
    }
}