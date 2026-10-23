If you find a bug, please open a new [issue](https://github.com/DavidBergstromSWE/GcLib/issues) and provide a detailed report.

Contributors are welcome to add new APIs to the library as well as improving existing ones, please fork the repository and add a pull request! 

> [!TIP]
> When adding a new API, derive the new device class from [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs), passing the class itself as `TDevice` (curiously recurring template pattern), and implement the [IDeviceFactory&lt;TDevice&gt;](src/Interfaces/IDeviceFactory.cs) interface.
> Example declaration of a new device class:
>
> ```csharp
> public sealed class MyCam : GcDevice<MyCam>, IDeviceFactory<MyCam>
> ```
>
> Some important notes regarding the device class design:
> - [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs) derives from the non-generic base class [GcDevice](src/Modules/GcDevice.cs), in which the common functionality for all device classes, including GenApi parameter access and image streaming, is implemented. 
> - Deriving directly from [GcDevice](src/Modules/GcDevice.cs) is not possible, as its constructor is inaccessible outside the library. Intermediate non-generic base classes are also not supported. The generic base class [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs) has been introduced to provide a strongly typed interface when deriving concrete device classes.
> - `IDeviceFactory<TDevice>` replaces the former `IDeviceEnumerator` and `IDeviceClassDescriptor` interfaces. It is implemented through static abstract members that control the lifetime of the device class API: `Initialize` (API initialization), `DeviceClassInfo` (API information), `EnumerateDevices` (device discovery and enumeration), `CreateDevice` (device instantiation) and `Release` (cleanup of API resources).
>	- `DeviceClassInfo` presents metadata about the device class API. Make sure it is readable without `Initialize` has been called, e.g. by using a static initializer.
>	- `Initialize` and `Release` may not be necessary for all APIs, leave them empty if not needed. `Initialize` will be called when the class is registered (`GcLibrary.Register`) and `Release` when the class is unregistered (`GcLibrary.Unregister`).
>	- `EnumerateDevices` and `CreateDevice` will only be useable after a successful registration.
> - When device classes are properly designed, they will be discovered automatically by `GcLibrary` when loaded in the current AppDomain.
> - Run tests to verify that your device class is working correctly (and add them, if available, to verify your device class implementation). The unit tests provided in [unit test project](tests/GcLib.Tests) only verify functionality of the library itself.

Make sure third-party binaries allow distribution before adding them to the repository!

For development purposes the library offers [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a physical device and enabling interaction with its GenApi parameters and streaming of image buffers.
