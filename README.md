Library providing a common interface to control settings and streaming of [GenICam/GenTL](https://www.emva.org/standards-technology/genicam/)-standardized cameras, using underlying APIs from supported third-party camera manufacturers and library providers.

Currently supported APIs are: 
[eBUS SDK](https://www.pleora.com/machine-vision-connectivity/ebus-sdk/),
[xiAPI.NET](https://www.ximea.com/support/wiki/apis/xiAPINET),
[Spinnaker SDK](https://www.teledynevisionsolutions.com/products/spinnaker-sdk/?model=Spinnaker%20SDK&vertical=machine%20vision&segment=iis) and
[PCO SDK](https://www.excelitas.com/product/pco-software-development-kits) (only partially implemented). Please note that some may require a license to use it on a particular system.

A simple [console app](samples/GcLib.Samples.ConsoleApp) has been added to demonstrate how to use the library. More sample demos (WinForms and WPF) are coming.

For development purposes the library implements [VirtualCam](src/APIs/VirtualCam) - a virtual camera simulator emulating a GeniCam device and enabling interaction with GenApi parameters and streaming of image buffers. 
