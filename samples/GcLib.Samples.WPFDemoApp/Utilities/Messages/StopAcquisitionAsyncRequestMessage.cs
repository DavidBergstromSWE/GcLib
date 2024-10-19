using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace ImagerViewer.Utilities.Messages;

/// <summary>
/// A message requesting all active acquisitions to stop, returning the task.
/// </summary>
internal sealed class StopAcquisitionAsyncRequestMessage : AsyncRequestMessage<Task> { }