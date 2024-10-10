Contributors are welcome to add new APIs to the library as well as improving existing ones, please fork the repository and add a pull request! 

> [!TIP]
> When adding a new API make sure the new device class implements the abstract base class [GcDevice](src/GcDevice.cs) and the following two interfaces: [IDeviceEnumerator](src/IDeviceEnumerator) (for device enumeration) and [IDeviceClassDescriptor](src/IDeviceClassDescriptor) (for providing device class information).

For bug reporting it is encouraged to write a detailed issue. 

Feature requests are welcome but it cannot be guaranteed that your request will be accepted. If accepted, no commitments are made regarding the timeline for its implementation and release.
