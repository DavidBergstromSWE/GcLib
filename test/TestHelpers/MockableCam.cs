using System;
using System.Collections.Generic;

namespace GcLib.UnitTests.Utilities;

/// <summary>
/// Abstract device type that can be mocked in unit tests. The non-generic <see cref="GcDevice"/> can't be derived from outside the library, so mocks are created from this class instead.
/// </summary>
public abstract class MockableCam : GcDevice<MockableCam>, IDeviceFactory<MockableCam>
{
    public static GcDeviceClassInfo DeviceClassInfo => new("MockableCam", "1.0", typeof(MockableCam));

    public static MockableCam CreateDevice(GcDeviceInfo deviceInfo) => throw new NotSupportedException();

    public static List<GcDeviceInfo> EnumerateDevices() => [];

    public static void Initialize() { }

    static void IDeviceFactory<MockableCam>.Release() { }
}
