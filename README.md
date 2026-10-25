# GcLib

GcLib is a .NET library that provides a common interface for discovering [GenICam/GenTL](https://www.emva.org/standards-technology/genicam/) cameras, accessing their parameters, and streaming image buffers. It wraps supported provider APIs so applications can use a consistent interface across camera vendors.

## Supported camera APIs

The following provider SDK versions are currently integrated in the repository:

| Provider | SDK | Integrated version |
| --- | --- | --- |
| [Pleora](https://www.pleora.com/machine-vision-connectivity/ebus-sdk/) | eBUS SDK | 6.4 |
| [Ximea](https://www.ximea.com/support/wiki/apis/xiAPINET) | xiAPI.NET | 4.33 |
| [Teledyne FLIR](https://www.teledynevisionsolutions.com/products/spinnaker-sdk/?model=Spinnaker%20SDK&vertical=machine%20vision&segment=iis) | Spinnaker SDK | 4.3 |
| [IDS](https://en.ids-imaging.com/ids-peak.html) | IDS Peak | 1.17 |
| [PCO](https://www.excelitas.com/product/pco-software-development-kits) | PCO SDK | 1.35 |
| [Basler](https://www.baslerweb.com/en/software/pylon/sdk/) | Pylon | 12.2 |
| [Allied Vision](https://www.alliedvision.com/en/support/software-downloads/vimba-x-sdk/vimba-x) | Vimba X | 1.3.4 |

These are the versions integrated in this repository, not a guarantee that every camera or SDK configuration has been tested. To use a physical camera, install the relevant provider SDK and camera driver. Some providers may also require a separate license. Check the provider's documentation for its system requirements and terms; third-party SDKs and dependencies are subject to their own licenses.

## Requirements

- .NET 10 SDK.
- A 64-bit Windows environment for the included samples and bundled native provider libraries. Individual provider SDKs may have additional operating system, driver, or hardware requirements.

## Quick start

From the repository root, run the Console sample:

```sh
dotnet run --project samples/GcLib.Samples.ConsoleApp
```

The [Console sample source](samples/GcLib.Samples.ConsoleApp) demonstrates library initialization, device discovery, parameter changes, and image acquisition, where a library-included camera emulator (VirtualCam) is used to demonstrate the workflow.

## Samples

- [Console app](samples/GcLib.Samples.ConsoleApp) — a minimal example of discovery, parameter access, and image acquisition.
- [WinForms app](samples/GcLib.Samples.WinFormsDemoApp) — a lightweight graphical demo.
- [WPF app](samples/GcLib.Samples.WPFDemoApp) — a fuller demo for discovering devices, changing parameters, viewing live streams, and basic recording and playback.

![Screenshot of the GcLib WPF demo application](samples/GcLib.Samples.WPFDemoApp/Resources/Images/WFPDemoApp_screenshot.png)

## Contributing

Contributions are welcome. See the [contribution guide](CONTRIBUTING.md) for project guidance. Before adding or distributing third-party binaries, check that their licenses allow it.

## License

GcLib is licensed under the GNU General Public License, version 3. See [LICENSE.md](LICENSE.md) for the full license text. Third-party SDKs and dependencies may have separate license terms.
