using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GcLib.Utilities.Numbers;

/// <summary>
/// Helper class for numeric types.
/// </summary>
public static class NumericHelper
{
    /// <summary>
    /// Determines whether type is numeric.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if numeric, false otherwise.</returns>
    public static bool IsNumericType(Type type)
    {
        if (type is null)
            return false;

        if (Type.GetTypeCode(type) is TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or TypeCode.Double or TypeCode.Single or TypeCode.Char)
            return true;

        // Check if the type implements INumber<T> interface for any T
        foreach (Type implementedInterface in type.GetInterfaces())
        {
            if (implementedInterface.IsGenericType &&
                implementedInterface.GetGenericTypeDefinition() == typeof(INumber<>) &&
                implementedInterface.GenericTypeArguments[0] == type)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns minimum value of numeric type.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <returns>Minimum value of type <see cref="T"/>.</returns>
    public static T GetMinValue<T>() where T : INumber<T>
    {
        return GetMinValue(typeof(T));
    }

    /// <summary>
    /// Returns minimum value of numeric type.
    /// </summary>
    /// <param name="type">Numeric type.</param>
    /// <returns>Minimum value of type.</returns>
    public static dynamic GetMinValue(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.SByte => Convert.ChangeType(sbyte.MinValue, type),
            TypeCode.Byte => Convert.ChangeType(byte.MinValue, type),
            TypeCode.Int16 => Convert.ChangeType(short.MinValue, type),
            TypeCode.UInt16 => Convert.ChangeType(ushort.MinValue, type),
            TypeCode.Int32 => Convert.ChangeType(int.MinValue, type),
            TypeCode.UInt32 => Convert.ChangeType(uint.MinValue, type),
            TypeCode.Int64 => Convert.ChangeType(long.MinValue, type),
            TypeCode.UInt64 => Convert.ChangeType(ulong.MinValue, type),
            TypeCode.Single => Convert.ChangeType(float.MinValue, type),
            TypeCode.Double => Convert.ChangeType(double.MinValue, type),
            TypeCode.Decimal => Convert.ChangeType(decimal.MinValue, type),
            TypeCode.Char => Convert.ChangeType(char.MinValue, type),
            _ => GetNumericBound(type, "MinValue"),
        };
    }

    /// <summary>
    /// Returns maximum value of numeric type.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <returns>Maximum value of type <see cref="T"/>.</returns>
    public static T GetMaxValue<T>() where T : INumber<T>
    {
        return GetMaxValue(typeof(T));
    }

    /// <summary>
    /// Returns maximum value of numeric type.
    /// </summary>
    /// <param name="type">Numeric type.</param>
    /// <returns>Maximum value of type.</returns>
    public static dynamic GetMaxValue(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.SByte => Convert.ChangeType(sbyte.MaxValue, type),
            TypeCode.Byte => Convert.ChangeType(byte.MaxValue, type),
            TypeCode.Int16 => Convert.ChangeType(short.MaxValue, type),
            TypeCode.UInt16 => Convert.ChangeType(ushort.MaxValue, type),
            TypeCode.Int32 => Convert.ChangeType(int.MaxValue, type),
            TypeCode.UInt32 => Convert.ChangeType(uint.MaxValue, type),
            TypeCode.Int64 => Convert.ChangeType(long.MaxValue, type),
            TypeCode.UInt64 => Convert.ChangeType(ulong.MaxValue, type),
            TypeCode.Single => Convert.ChangeType(float.MaxValue, type),
            TypeCode.Double => Convert.ChangeType(double.MaxValue, type),
            TypeCode.Decimal => Convert.ChangeType(decimal.MaxValue, type),
            TypeCode.Char => Convert.ChangeType(char.MaxValue, type),
            _ => GetNumericBound(type, "MaxValue"),
        };
    }

    /// <summary>
    /// Converts an array of numeric values to a byte array in native byte order.
    /// </summary>
    /// <typeparam name="T">The unmanaged numeric type of the values.</typeparam>
    /// <param name="numericArray">The numeric array to convert.</param>
    /// <returns>A copy of the byte representation of <paramref name="numericArray"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="numericArray"/> is null.</exception>
    public static byte[] ToBytes<T>(T[] numericArray) where T : unmanaged, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(numericArray);

        // Create a ReadOnlySpan<byte> view over the numeric array without copying data.
        ReadOnlySpan<byte> bytes = MemoryMarshal.Cast<T, byte>(numericArray.AsSpan());

        return bytes.ToArray();
    }

    /// <summary>
    /// Converts a byte array to an array of numeric values.
    /// </summary>
    /// <typeparam name="T">The unmanaged numeric type of the values.</typeparam>
    /// <param name="bytes">The byte array to convert.</param>
    /// <returns>An array of values converted from <paramref name="bytes"/>.</returns>
    /// <exception cref="ArgumentException">The length of <paramref name="bytes"/> is not a multiple of the size of <typeparamref name="T"/>.</exception>
    public static T[] ToArray<T>(byte[] bytes) where T : unmanaged, INumber<T>
    {
        ArgumentNullException.ThrowIfNull(bytes);

        // Ensure that the byte array length is a multiple of the size of the numeric type T.
        if (bytes.Length % Unsafe.SizeOf<T>() != 0)
            throw new ArgumentException("Byte count must be a multiple of the numeric type's size.", nameof(bytes));

        // Create a ReadOnlySpan<T> view over the byte array without copying data.
        ReadOnlySpan<T> values = MemoryMarshal.Cast<byte, T>(bytes.AsSpan());

        return values.ToArray();
    }

    /// <summary>
    /// Converts a double value to an integer representation in a pre-allocated byte-span of the same size as the integral numeric type.
    /// <para>
    /// Note: The integer type is inferred from the number of bytes allocated in the referenced byte-span. If it cannot be inferred an <see cref="InvalidCastException"/> will be thrown.
    /// </para>
    /// </summary>
    /// <param name="bytes">Reference to a pre-allocated byte-span of the same size as the numeric integral type being the target of the conversion.</param>
    /// <param name="value">Value to be converted.</param>
    /// <exception cref="InvalidCastException"></exception>
    public static void DoubleToSpan(ref Span<byte> bytes, double value)
    {
        switch (bytes.Length)
        {
            case 1:
                bytes[0] = (byte)value;
                break;
            case 2:
                BitConverter.TryWriteBytes(bytes, (ushort)value);
                break;
            case 4:
                BitConverter.TryWriteBytes(bytes, (uint)value);
                break;
            case 8:
                BitConverter.TryWriteBytes(bytes, (ulong)value);
                break;
            default:
                throw new InvalidCastException($"Could not find a suitable integral numeric type of size {bytes.Length} bytes! Please check memory allocation.");
        }
    }

    /// <summary>
    /// Converts an array of double values to their integer representations in a single pre-allocated byte-span.
    /// <para>
    /// Note: The integer type is inferred from the number of bytes allocated in the referenced byte-span. If it cannot be inferred an <see cref="InvalidCastException"/> will be thrown.
    /// </para>
    /// </summary>
    /// <param name="bytes">Pre-allocated byte-span sized for all values at the target integral type's width.</param>
    /// <param name="values">Values to be converted.</param>
    /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
    /// <exception cref="InvalidCastException">The span length does not match a supported integral width for all values.</exception>
    public static void DoubleToSpan(ref Span<byte> bytes, double[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        if (values.Length == 0)
        {
            if (bytes.IsEmpty)
                return;

            throw new InvalidCastException($"Could not find a suitable integral numeric type of size {bytes.Length} bytes! Please check memory allocation.");
        }

        if (bytes.Length % values.Length != 0)
            throw new InvalidCastException($"Could not find a suitable integral numeric type of size {bytes.Length} bytes! Please check memory allocation.");

        int bytesPerValue = bytes.Length / values.Length;
        if (bytesPerValue is not (1 or 2 or 4 or 8))
            throw new InvalidCastException($"Could not find a suitable integral numeric type of size {bytes.Length} bytes! Please check memory allocation.");

        for (int i = 0; i < values.Length; i++)
        {
            Span<byte> destination = bytes.Slice(i * bytesPerValue, bytesPerValue);
            DoubleToSpan(ref destination, values[i]);
        }
    }

    /// <summary>
    /// Converts a read-only span of bytes to the integral numeric type representation of the buffer and returns it casted to a double.
    /// <para>
    /// Supported types are <see cref="byte"/>, <see cref="ushort"/>, <see cref="uint"/> and <see cref="ulong"/>.
    /// </para>
    /// </summary>
    /// <param name="bytes">Read-only byte-span representing an integral numeric type of the buffer.</param>
    /// <returns>Integer representation of byte-span as a double. </returns>
    /// <exception cref="InvalidCastException"></exception>
    public static double SpanToDouble(ReadOnlySpan<byte> bytes)
    {
        // Convert byte-span to integral numeric type according to bit depth (span length) and implicitly convert it to double.
        return bytes.Length switch
        {
            1 => bytes[0],                      // 8-bit
            2 => BitConverter.ToUInt16(bytes),  // 16-bit
            4 => BitConverter.ToUInt32(bytes),  // 32-bit
            8 => BitConverter.ToUInt64(bytes),  // 64-bit (warning: may loose precision!)
            _ => throw new InvalidCastException("Byte-span can only be of lengths 1, 2, 4 or 8!")
        };
    }

    /// <summary>
    /// Gets a numeric bound from a public static field or property.
    /// </summary>
    /// <param name="type">The numeric type to search.</param>
    /// <param name="boundName">The name of the public static field or property containing the bound.</param>
    /// <returns>The value of the specified bound.</returns>
    /// <exception cref="InvalidOperationException">The type is not numeric or does not define the specified public static field or property.</exception>
    private static object GetNumericBound(Type type, string boundName)
    {
        if (IsNumericType(type))
        {
            // Try to get the public static field or property with the specified name.
            var field = type.GetField(boundName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (field is not null)
                return field.GetValue(null);

            // If the field is not found, try to get the public static property with the specified name.
            var property = type.GetProperty(boundName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (property is not null)
                return property.GetValue(null);
        }

        throw new InvalidOperationException("Type not supported.");
    }
}