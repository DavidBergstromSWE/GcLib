If you find a bug, please open a new [issue](https://github.com/DavidBergstromSWE/GcLib/issues) and provide a detailed report.

Contributors are welcome to add new APIs to the library as well as improving existing ones, please fork the repository and add a pull request! 

> [!TIP]
> When adding a new API make sure the new device class implements the abstract base class [GcDevice](src/GcDevice.cs) and the following two interfaces: [IDeviceEnumerator](src/IDeviceEnumerator) (for device enumeration) and [IDeviceClassDescriptor](src/IDeviceClassDescriptor) (for providing device class information).

For development purposes the library implements [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a GeniCam device and enabling interaction with its GenApi parameters and streaming of image buffers.
