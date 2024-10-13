using CommunityToolkit.Mvvm.Messaging.Messages;
using FusionViewer.Models;

namespace FusionViewer.Utilities.Messages;

/// <summary>
/// A message requesting currently selected image channel for display.
/// </summary>
internal sealed class SelectedChannelRequestMessage : RequestMessage<ImageModel> { }