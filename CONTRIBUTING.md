If you find a bug, please open a new [issue](https://github.com/DavidBergstromSWE/GcLib/issues) and provide a detailed report.

Contributors are welcome to add new APIs to the library as well as improving existing ones, please fork the repository and add a pull request! 

> [!TIP]
> When adding a new API, derive the new device class from [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs), passing the class itself as `TDevice` (curiously recurring template pattern), and implement the [IDeviceFactory&lt;TDevice&gt;](src/Interfaces/IDeviceFactory.cs) interface:
>
> ```csharp
> public sealed class MyCam : GcDevice<MyCam>, IDeviceFactory<MyCam>
> ```
>
> - [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs) derives from the non-generic base class [GcDevice](src/Modules/GcDevice.cs), in which the common functionality for all device classes, including GenApi parameter access and image streaming is implemented. 
> - Deriving directly from [GcDevice](src/Modules/GcDevice.cs) is not possible, as its constructor is inaccessible outside the library. Intermediate non-generic base classes are also not supported. The generic base class [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs) is used in order to provide a strongly typed interface when deriving concrete device classes.
> - `IDeviceFactory<TDevice>` replaces the former `IDeviceEnumerator` and `IDeviceClassDescriptor` interfaces. It is implemented through static abstract members that control the lifetime of the device class API: `Initialize` (API initialization), `DeviceClassInfo` (API information), `CreateDevice` (device instantiation), `EnumerateDevices` (device discovery and enumeration) and `Release` (cleanup of API resources).
> - Device classes are discovered automatically by `GcLibrary` when loaded in the current AppDomain. `Initialize` is called when the class is registered, and `Release` is called when the class is unregistered.

For development purposes the library offers [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a physical device and enabling interaction with its GenApi parameters and streaming of image buffers.
