If you find a bug, please open a new [issue](https://github.com/DavidBergstromSWE/GcLib/issues) and provide a detailed report.

Contributors are welcome to add new APIs to the library as well as improving existing ones, please fork the repository and add a pull request! 

> [!TIP]
> When adding a new API, derive the new device class from [GcDevice&lt;TDevice&gt;](src/Modules/GcDeviceOfT.cs), passing the class itself as `TDevice` (curiously recurring template pattern), and implement the [IDeviceFactory&lt;TDevice&gt;](src/Interfaces/IDeviceFactory.cs) interface:
>
> ```csharp
> public sealed class MyCam : GcDevice<MyCam>, IDeviceFactory<MyCam>
> {
>     public static void Initialize() { /* initialize the API/SDK */ }
>     public static GcDeviceClassInfo DeviceClassInfo { get; } = new("MyApi", "1.0.0", typeof(MyCam));
>     public static MyCam CreateDevice(GcDeviceInfo deviceInfo) => new(deviceInfo);
>     public static List<GcDeviceInfo> EnumerateDevices() => /* list available devices */;
>     public static void Release() { /* release shared API/SDK resources */ }
> }
> ```
>
> - Deriving directly from the abstract base class [GcDevice](src/Modules/GcDevice.cs) is not possible, as its constructor is not accessible outside the library. Intermediate non-generic base classes are not supported, since `TDevice` must be the concrete device class itself.
> - `IDeviceFactory<TDevice>` replaces the former `IDeviceEnumerator` and `IDeviceClassDescriptor` interfaces. It is implemented through static abstract members: `Initialize` (API initialization), `DeviceClassInfo` (device class information, whose `DeviceType` must equal the class itself), `CreateDevice`, `EnumerateDevices` and `Release` (cleanup).
> - Device classes are discovered and registered automatically by `GcLibrary` when loaded in the current AppDomain. `Initialize` is called when the class is registered, and registration is rolled back (and `Release` called) if initialization or enumeration fails.

For development purposes the library implements [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a GeniCam device and enabling interaction with its GenApi parameters and streaming of image buffers.
