using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Modules;

[TestClass]
public class GcDeviceInheritanceTests
{
    [TestMethod]
    public void LibraryAssembly_NoTypeDerivesDirectlyFromNonGenericGcDevice()
    {
        var offendingTypes = typeof(GcDevice).Assembly.GetTypes()
            .Where(type => type.BaseType == typeof(GcDevice) && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GcDevice<>)))
            .Select(type => type.FullName)
            .ToList();

        // GcDevice<TDevice> is the only permitted direct subclass; device classes must derive from it.
        CollectionAssert.AreEqual(Array.Empty<string>(), offendingTypes, $"Types deriving directly from GcDevice: {string.Join(", ", offendingTypes)}");
    }

    [TestMethod]
    public void GcDeviceOfT_IsTheOnlyDirectSubclassOfGcDevice()
    {
        var directSubclasses = typeof(GcDevice).Assembly.GetTypes()
            .Where(type => type.BaseType == typeof(GcDevice))
            .ToList();

        Assert.HasCount(1, directSubclasses);
        Assert.AreEqual(typeof(GcDevice<>), directSubclasses[0]);
    }

    [TestMethod]
    public void DevicesDerivingFromGcDeviceOfT_PassThemselvesAsTypeArgument()
    {
        var offendingTypes = new[] { typeof(GcDevice).Assembly, typeof(GcDeviceInheritanceTests).Assembly }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.BaseType is { IsGenericType: true } baseType
                           && baseType.GetGenericTypeDefinition() == typeof(GcDevice<>)
                           && baseType.GetGenericArguments()[0] != type)
            .Select(type => type.FullName)
            .ToList();

        CollectionAssert.AreEqual(Array.Empty<string>(), offendingTypes, $"Types not passing themselves to GcDevice<T>: {string.Join(", ", offendingTypes)}");
    }
}
