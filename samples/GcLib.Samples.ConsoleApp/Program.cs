using GcLib;

/* The simple example shows how to initialize the library, discover available devices and acquire images from a virtual camera simulator. The app should produce two images in the output directory. */

// Library always needs to be initialized before it can be used. Initialization will discover which APIs are supported on the current system.
GcLibrary.Init();

// Instantiate system.
using var system = new GcSystem();

// Ask system to enumerate available devices.
system.UpdateDeviceList();
var cameraList = system.GetDeviceList();

// Pick the first available one (will be an instance of VirtualCam).
GcDevice camera = system.OpenDevice(cameraList[0]);

// Fetch a single image and save it to file (via Emgu.CV). Image should show a grayscale image (320x240, 8-bit) of a vertical ramp moving testpattern.
camera.FetchImage(out GcBuffer image);
image.ToMat().Save("image1.tiff");

// Update some camera parameters.
camera.Parameters.SetParameterValue("Height", "480");
camera.Parameters.SetParameterValue("Width", "640");
camera.Parameters.SetParameterValue("PixelFormat", "RGB8");
camera.Parameters.SetParameterValue("TestPattern", "FrameCounter");

// Open a new datastream (to allow continuous acquisition).
GcDataStream dataStream = camera.OpenDataStream();

// Start streaming images from the camera.
dataStream.Start();

// Wait for the acquisition to start to allow image retrieval. Save it. Image should show a RGB image (640x480, 8-bit) of a frame counter testpattern (frame count should be 1).
Thread.Sleep(100);
dataStream.RetrieveImage(out image);
image.ToMat().Save("image2.tiff");

// Close camera (automatically closes datastream).
camera.Close();

// Close library.
GcLibrary.Close();