using SpinnakerNET;

namespace GcLib;

/// <summary>
/// Provides extension methods for an <see cref="IManagedCameraList"/>.
/// </summary>
public static class ManagedCameraListExtensions
{
    /// <summary>
    /// Retrieves the first managed camera from the list that matches the specified unique device identifier.
    /// </summary>
    /// <remarks>If multiple cameras share the same unique device identifier, only the first match in the list
    /// is returned.</remarks>
    /// <param name="cameraList">The list of managed cameras to search for a matching device identifier.</param>
    /// <param name="uniqueID">The unique device identifier to locate within the camera list. Cannot be null.</param>
    /// <returns>An instance of <see cref="IManagedCamera"/> that matches the specified unique device identifier; otherwise, <see
    /// langword="null"/> if no match is found.</returns>
    public static IManagedCamera GetByUniqueID(this IManagedCameraList cameraList, string uniqueID)
    {
        foreach (var camera in cameraList)
        {
            if (camera.GetDeviceID() == uniqueID)
                return camera;
        }
        return null;
    }
}