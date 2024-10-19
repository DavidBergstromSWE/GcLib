using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ImagerViewerApp.Utilities.Messages;

/// <summary>
/// A message requesting playback status. The message returns true if a playback sequence is loading or has been loaded.
/// </summary>
internal sealed class PlayBackStatusRequestMessage : RequestMessage<bool> { }