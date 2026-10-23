If you find a bug, please open a new [issue](https://github.com/DavidBergstromSWE/GcLib/issues) and provide a detailed report.

Contributors are welcome to add new APIs to the library as well as improving existing ones, please fork the repository and add a pull request! 

> [!TIP]
> When adding a new API, derive the new device class from [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs), passing the class itself as `TDevice` (curiously recurring template pattern), and implement the [IDeviceFactory&lt;TDevice&gt;](src/Interfaces/IDeviceFactory.cs) interface.
> Example declaration of a new device class:
>
> ```csharp
> public sealed class MyCam : GcDevice<MyCam>, IDeviceFactory<MyCam>
> ```

Some important notes regarding the device class design:
- [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs) derives from the non-generic base class [GcDevice](src/Modules/GcDevice.cs), which provides the GenICam functionality including GenApi parameter access and GenTL device discovery and image data streaming. Deriving directly from [GcDevice](src/Modules/GcDevice.cs) is not possible, as its constructor is inaccessible outside the library. Intermediate non-generic base classes are also not supported. [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs) has been introduced to provide a strongly typed interface when deriving concrete device classes, ensuring type safety and proper integration with the library's device management system.
- [IDeviceFactory&lt;TDevice&gt;](src/Interfaces/IDeviceFactory.cs) contains static abstract members that control the lifetime of the device class API:
	- `Initialize` and `Release` may be used for initialization and resource cleanup of the device class API (they may not be necessary for all APIs, leave them empty if not needed). The methods will automatically be called when the device class is registered and unregistered in the library.
	- `DeviceClassInfo` presents metadata about the device class API. Make sure it is readable without `Initialize` has been called, e.g. by using a static initializer.
	- `EnumerateDevices` and `CreateDevice` enables discovery/enumeration and instantiation of devices using the device class API. These methods can only be expected to work after a successful registration, otherwise they may result in exceptions being thrown.
- When device classes are properly designed, they will be discovered automatically by the library when loaded in the current AppDomain and should be found in e.g. `GcLibrary.GetImplementedDeviceClasses`.
- Device classes can be registered automatically during library initialization (`GcLibrary.Init`) and unregistered automatically when the library is closed (`GcLibrary.Close`). If control of lifetime of a device class API is needed, device classes can be registered and unregistered explicitly using `GcLibrary.Register` and `GcLibrary.Unregister`.

Before requesting a PR, please run tests to verify that your device class is working correctly (if available, add them to verify your implementation). The unit tests provided in [unit test project](tests/GcLib.Tests) currently only verify functionality of the library itself.

Make sure third-party binaries allow distribution before adding them to the repository!

For development purposes the library offers [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a physical device and enabling interaction with its GenApi parameters and streaming of image buffers.
