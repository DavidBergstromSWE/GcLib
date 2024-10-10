Library providing a common interface to control settings and streaming of [GenICam/GenTL](https://www.emva.org/standards-technology/genicam/)-standardized cameras in .NET, using underlying APIs from supported third-party providers.
GcLib essentially works like a wrapper around the external APIs, hiding their specific implementations and enabling the same code to be used regardless of the camera or vendor.

Currently supported APIs are: 
[eBUS SDK](https://www.pleora.com/machine-vision-connectivity/ebus-sdk/),
[xiAPI.NET](https://www.ximea.com/support/wiki/apis/xiAPINET),
[Spinnaker SDK](https://www.teledynevisionsolutions.com/products/spinnaker-sdk/?model=Spinnaker%20SDK&vertical=machine%20vision&segment=iis) and
[PCO SDK](https://www.excelitas.com/product/pco-software-development-kits) (partially implemented). 

> [!NOTE]
> Please note that some APIs may require a license.

A simple [console app](samples/GcLib.Samples.ConsoleApp) has been added to demonstrate how to use the library. More sample demos are coming (WinForms and WPF).

For development purposes the library also implements [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a GeniCam device and enabling interaction with its GenApi parameters and streaming of image buffers.

Contributors are welcome to the project, please see this [short guide](CONTRIBUTING.md) for further info.
