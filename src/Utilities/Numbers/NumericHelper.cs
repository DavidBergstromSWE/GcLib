using System;
using System.Numerics;
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
    /// <param name="type">System.Type.</param>
    /// <returns>True if numeric, false otherwise.</returns>
    public static bool IsNumericType(Type type)
    {
        return Type.GetTypeCode(type) switch
        {
            TypeCode.Byte or TypeCode.SByte or TypeCode.UInt16 or TypeCode.UInt32 or TypeCode.UInt64 or TypeCode.Int16 or TypeCode.Int32 or TypeCode.Int64 or TypeCode.Decimal or TypeCode.Double or TypeCode.Single or TypeCode.Char => true,
            _ => false,
        };
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
            _ => throw new InvalidOperationException("Type not supported."),
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
            _ => throw new InvalidOperationException("Type not supported."),
        };
    }

    /// <summary>
    /// Converts generic numeric array to byte array.
    /// </summary>
    /// <typeparam name="T">Generic numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="numericArray">Generic numeric array.</param>
    /// <returns>Byte array representation of generic numeric array.</returns>
    public static byte[] ToBytes<T>(T[] numericArray) where T : INumber<T>
    {
        byte[] bytes = new byte[numericArray.Length * Marshal.SizeOf<T>()];
        Buffer.BlockCopy(numericArray, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Converts byte array to numeric array of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Numeric type (byte, ushort, uint, float, double, etc.).</typeparam>
    /// <param name="bytes">Byte array.</param>
    /// <returns>Numeric array.</returns>
    public static T[] ToArray<T>(byte[] bytes) where T : INumber<T>
    {
        T[] values = new T[bytes.Length / Marshal.SizeOf<T>()];
        Buffer.BlockCopy(bytes, 0, values, 0, bytes.Length);
        return values;
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
        // Infer integral type from number of bytes.
        if (bytes.Length == 1)      // byte
            bytes[0] = (byte)value;
        else if (bytes.Length == 2) // ushort
            bytes = BitConverter.GetBytes((ushort)value);
        else if (bytes.Length == 4) // uint
            bytes = BitConverter.GetBytes((uint)value);
        else if (bytes.Length == 8) // ulong
            bytes = BitConverter.GetBytes((ulong)value);
        else throw new InvalidCastException($"Could not find a suitable integral numeric type of size {bytes.Length} bytes! Please check memory allocation.");
    }

    /// <summary>
    /// Converts an array of double values to their integer representations in a single pre-allocated byte-span.
    /// <para>
    /// Note: The integer type is inferred from the number of bytes allocated in the referenced byte-span. If it cannot be inferred an <see cref="InvalidCastException"/> will be thrown.
    /// </para>
    /// </summary>
    /// <param name="bytes">Reference to a pre-allocated byte-span of the same size as the numeric integral type being the target of the conversion multiplied by the number of values to be converted.</param>
    /// <param name="values">Values to be converted.</param>
    /// <exception cref="InvalidCastException"></exception>
    public static void DoubleToSpan(ref Span<byte> bytes, double[] values)
    {
        if (bytes.Length == values.Length)
        {
            for (int i = 0; i < values.Length; i++)
                bytes[i] = (byte)values[i];
        }
        else if (bytes.Length == values.Length * 2)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Span<byte> u = BitConverter.GetBytes((ushort)values[i]);
                for (int j = 0; j < u.Length; j++)
                    bytes[i * 2 + j] = u[j];
            }
        }
        else if (bytes.Length == values.Length * 4)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Span<byte> u = BitConverter.GetBytes((uint)values[i]);
                for (int j = 0; j < u.Length; j++)
                    bytes[i * 4 + j] = u[j];
            }
        }
        else if (bytes.Length == values.Length * 8)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Span<byte> u = BitConverter.GetBytes((ulong)values[i]);
                for (int j = 0; j < u.Length; j++)
                    bytes[i * 8 + j] = u[j];
            }
        }
        else throw new InvalidCastException($"Could not find a suitable integral numeric type of size {bytes.Length} bytes! Please check memory allocation.");
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
}