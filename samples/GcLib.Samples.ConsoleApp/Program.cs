using GcLib;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;

/* The simple example shows how to initialize the library, discover available devices, change device parameters and acquire images from it. The app should produce two images in the output directory. */

// Configure application logger.
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("Application started");

// Initialization of the library will discover which APIs are supported on the current system (always needed before the library can be used).
GcLibrary.Init(logger: new SerilogLoggerFactory(Log.Logger).CreateLogger<GcSystem>());

// Instantiate system level, from which devices can be opened (note: only one system level can be used at a time!).
using var system = new GcSystem();

// Ask system to enumerate available devices.
system.UpdateDeviceList();
var cameraList = system.GetDeviceList();

// Open the first available device (will be an instance of a virtual camera simulator in this case).
GcDevice camera = system.OpenDevice(cameraList[0]);

// Fetch a single image and save it to file (via Emgu.CV.Mat format). Image should be a grayscale image of a vertical ramp moving testpattern (320x240 pixels, 8-bit).
camera.FetchImage(out GcBuffer image);
image.ToMat().Save("image1.tiff");
Log.Information("image1.tiff saved to {dir}", Directory.GetCurrentDirectory());

// Update some camera parameters (for a complete list of available parameters see camera.Parameters.ToList()).
camera.Parameters.SetParameterValue("Height", "480");
camera.Parameters.SetParameterValue("Width", "640");
camera.Parameters.SetParameterValue("PixelFormat", "RGB8");
camera.Parameters.SetParameterValue("TestPattern", "FrameCounter");

// Open a new datastream (to allow continuous acquisition).
GcDataStream dataStream = camera.OpenDataStream();

// Start streaming images from the camera.
dataStream.Start();

// Suspend current thread to allow at least one image to be queued up. 
Thread.Sleep(100);

// Retrieve image from the front of the queue and save it to file. Image should be an RGB image of a frame counter testpattern with counter set to 1 (640x480, 8-bit).
dataStream.RetrieveImage(out image); // alternatively, images can be retrieved by subscribing to the dataStream.BufferTransferred event. 
image.ToMat().Save("image2.tiff");
Log.Information("image2.tiff saved to {dir}", Directory.GetCurrentDirectory());

// Close camera (note: this will also stop all streaming).
camera.Close();

// Close library.
GcLibrary.Close();

Log.Information("Application stopped");