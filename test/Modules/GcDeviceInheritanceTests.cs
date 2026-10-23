using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GcLib.UnitTests.Modules;

[TestClass]
public class GcDeviceInheritanceTests
{
    /// <summary>
    /// Verifies that no type in the library assembly derives directly from <see cref="GcDevice"/> except <see
    /// cref="GcDevice{TDevice}"/>.
    /// </summary>
    [TestMethod]
    public void LibraryAssembly_NoTypeDerivesDirectlyFromNonGenericGcDevice()
    {
        var offendingTypes = typeof(GcDevice).Assembly.GetTypes()
            .Where(type => type.BaseType == typeof(GcDevice) && !(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GcDevice<>)))
            .Select(type => type.FullName)
            .ToList();

        // GcDevice<TDevice> is the only permitted direct subclass; device classes must derive from it.
        Assert.AreSequenceEqual([], offendingTypes, $"Types deriving directly from GcDevice: {string.Join(", ", offendingTypes)}");
    }

    /// <summary>
    /// Verifies that GcDevice<T> is the only direct subclass of GcDevice.
    /// </summary>
    [TestMethod]
    public void GcDeviceOfT_IsTheOnlyDirectSubclassOfGcDevice()
    {
        var directSubclasses = typeof(GcDevice).Assembly.GetTypes()
            .Where(type => type.BaseType == typeof(GcDevice))
            .ToList();

        Assert.HasCount(1, directSubclasses);
        Assert.AreEqual(typeof(GcDevice<>), directSubclasses[0]);
    }

    /// <summary>
    /// Verifies that each type deriving from <see cref="GcDevice{T}"/> passes itself as the generic type argument.
    /// </summary>
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

        Assert.AreSequenceEqual([], offendingTypes, $"Types not passing themselves to GcDevice<T>: {string.Join(", ", offendingTypes)}");
    }
}
