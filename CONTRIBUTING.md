If you find a bug, please open a new [issue](https://github.com/DavidBergstromSWE/GcLib/issues) and provide a detailed report.

Contributors are welcome to add new APIs to the library as well as improving existing ones, please fork the repository and add a pull request! 

> [!TIP]
> When adding a new API, derive the new device class from [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs), passing the class itself as `TDevice` (curiously recurring template pattern), and implement the [IDeviceFactory&lt;TDevice&gt;](src/Interfaces/IDeviceFactory.cs) interface:
>
> ```csharp
> public sealed class MyCam : GcDevice<MyCam>, IDeviceFactory<MyCam>
> ```
>
> - Deriving directly from the non-generic base class [GcDevice](src/Modules/GcDevice.cs) is not possible, as its constructor is not accessible outside the library. Intermediate non-generic base classes are not supported, since `TDevice` must be the concrete device class itself.
> - `IDeviceFactory<TDevice>` replaces the former `IDeviceEnumerator` and `IDeviceClassDescriptor` interfaces. It is implemented through static abstract members: `Initialize` (API initialization), `DeviceClassInfo` (device class information), `CreateDevice` (device instantiation), `EnumerateDevices` (device discovery and enumeration) and `Release` (resource cleanup).
> - Device classes are discovered and registered automatically by `GcLibrary` when loaded in the current AppDomain. `Initialize` is called when the class is registered, and `Release` is called when the class is unregistered.

For development purposes the library offers [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a physical device and enabling interaction with its GenApi parameters and streaming of image buffers.
