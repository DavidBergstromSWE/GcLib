using System;

namespace GcLib.Utilities.Numbers;

/// <summary>
/// Provides extension methods for manipulating byte arrays, including bit extraction/insertion operations.
/// </summary>
/// <remarks>This class includes methods that allow for the extraction and insertion of specific ranges of bits
/// within byte arrays, accommodating different endianness and bit numbering conventions. It is useful in scenarios
/// involving low-level data manipulation, such as serialization and communication protocols.</remarks>
public static class ByteExtensions
{
    /// <summary>
    /// Specifies the byte order used to represent data in memory.
    /// </summary>
    /// <remarks>This enumeration is commonly used in data serialization and network communication to indicate
    /// the endianness of data. Big-endian format stores the most significant byte at the smallest memory address, while
    /// little-endian format stores the least significant byte first.</remarks>
    public enum Endianness 
    {
        /// <summary>
        /// LSB (least significant byte) first.
        /// </summary>
        LittleEndian,
        /// <summary>
        /// MSB (most significant byte) first.
        /// </summary>
        BigEndian
    }

    /// <summary>
    /// Specifies the bit numbering convention used to interpret the order of bits within a binary value.
    /// </summary>
    /// <remarks>Use this enumeration to indicate whether the least significant bit (Lsb0) or the most
    /// significant bit (Msb0) is considered bit zero. The choice of bit numbering affects how binary data is read,
    /// written, or displayed in various applications, such as image processing or communication protocols.</remarks>
    public enum BitNumbering 
    { 
        /// <summary>
        /// Represents the least significant bit (LSB) position in a binary value, where the rightmost bit is considered bit zero.
        /// </summary>
        Lsb0,
        /// <summary>
        /// Represents the most significant bit (MSB) position in a binary value, where the leftmost bit is considered bit zero.
        /// </summary>
        Msb0
    }

    /// <summary>
    /// Represents the number of bits in a single byte. This value is always 8.
    /// </summary>
    public const int BitsPerByte = 8;

    /// <summary>
    /// Extracts a specified range of bits from the input byte array and returns them as a new byte array.
    /// </summary>
    /// <remarks>The extracted bits are packed into the result array according to the specified endianness and
    /// bit numbering scheme. The method does not modify the input array.</remarks>
    /// <param name="bytes">The source byte array from which bits are to be extracted. Cannot be null.</param>
    /// <param name="start">The zero-based index, in bits, at which to begin extraction. Must be non-negative and less than the total number
    /// of bits in the input array.</param>
    /// <param name="length">The number of bits to extract. Must be non-negative and the range defined by start and length must not exceed
    /// the total number of bits in the input array.</param>
    /// <param name="endianness">Specifies the byte order to use when extracting bits from the input array.</param>
    /// <param name="bitNumbering">Specifies the bit numbering scheme to use when extracting bits (most significant bit first or least significant
    /// bit first).</param>
    /// <returns>A new byte array containing the extracted bits, packed into bytes. If the number of bits is not a multiple of 8,
    /// the last byte will be partially filled.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when start is negative or not less than the total number of bits in the input array, when length is
    /// negative, or when the range defined by start and length exceeds the total number of bits in the input array.</exception>
    public static byte[] GetBitRange(this byte[] bytes, int start, int length, Endianness endianness = Endianness.LittleEndian, BitNumbering bitNumbering = BitNumbering.Lsb0)
    {
        // Calculate the length of the input byte array in bits.
        int maxLength = bytes.Length * BitsPerByte;

        // Calculate the end index of the bit range.
        int end = start + length;

        // Range validations.
        if (start >= maxLength || start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), start, $"Start must non-negative and lesser than {maxLength}");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Length must be non-negative");
        if (end > maxLength)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Range length must be less than or equal to {maxLength}");

        // Calculate the length of the byte array for the result.
        var (byteLength, remainderLength) = Math.DivRem(length, BitsPerByte);

        // Allocate new result array.
        byte[] result = new byte[byteLength + (remainderLength == 0 ? 0 : 1)];

        // Iterate through each byte in the result array.
        for (int i = 0; i < result.Length; i++)
        {
            // Compute each of the 8 bits of the ith byte.
            // Stop if start index >= end index (rest of the bits in the current byte will be 0 by default).
            for (int j = 0; j < BitsPerByte && start < end; j++)
            {
                // Calculate the source byte and bit indices.
                var (sourceByteIndex, sourceBitIndex) = Math.DivRem(start++, BitsPerByte); // Note the increment(++) in start

                // Current bit index in the result byte.
                int resultBitIndex = j;

                // Adjust for MSB 0.
                if (bitNumbering is BitNumbering.Msb0)
                {
                    resultBitIndex = 7 - resultBitIndex; // Adjust result bit index
                    sourceBitIndex = 7 - sourceBitIndex; // Adjust source bit index
                }
                // Adjust source byte index for little-endian.
                if (endianness is Endianness.LittleEndian)
                    sourceByteIndex = bytes.Length - 1 - sourceByteIndex; // Adjust byte index of source

                // Set the current bit in the result byte.
                result[i] |= (byte)(((bytes[sourceByteIndex] >> sourceBitIndex) & 1) << resultBitIndex);
            }
        }

        return result;
    }

    /// <summary>
    /// Inserts a specified range of bits in the target byte array to values from another byte array, using the given
    /// endianness and bit numbering scheme.
    /// </summary>
    /// <remarks>This method modifies the contents of the target byte array in place. The bit range is set
    /// according to the specified endianness and bit numbering scheme, which affects how bits are mapped between the
    /// source and target arrays.</remarks>
    /// <param name="bytes">The byte array in which the bits will be set.</param>
    /// <param name="start">The zero-based index of the first bit to set within the target byte array.</param>
    /// <param name="length">The number of bits to set, starting from the specified start index.</param>
    /// <param name="value">The bytes containing the bit values to set in the specified range.</param>
    /// <param name="endianness">Specifies the endianness to use when interpreting the target byte array.</param>
    /// <param name="bitNumbering">Specifies the bit numbering scheme to use, such as most significant bit first (Msb0) or least significant bit
    /// first.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start index or length is negative, or if the specified range exceeds the bounds of the target
    /// byte array.</exception>
    public static void SetBitRange(this byte[] bytes, int start, int length, Span<byte> value, Endianness endianness = Endianness.LittleEndian, BitNumbering bitNumbering = BitNumbering.Lsb0)
    {
        // Calculate the length of the input byte array in bits.
        int maxLength = bytes.Length * BitsPerByte;

        // Calculate the end index of the bit range.
        int end = start + length;

        // Range validations.
        if (start >= maxLength || start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), start, $"Start must non-negative and lesser than {maxLength}");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Length must be non-negative");
        if (end > maxLength)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Range length must be less than or equal to {maxLength}");

        // Iterate through each byte in the value byte array.
        for (int i = 0; i < value.Length; i++)
        {
            // Compute each of the 8 bits of the ith byte.
            // Stop if start index >= end index (rest of the bits in the current byte will be 0 by default).
            for (int j = 0; j < BitsPerByte && start < end; j++)
            {
                // Calculate the target byte and bit indices.
                var (targetByteIndex, targetBitIndex) = Math.DivRem(start++, BitsPerByte); // Note the increment(++) in start

                // Current bit index in the source byte.
                int sourceBitIndex = j;

                // Adjust for MSB 0.
                if (bitNumbering is BitNumbering.Msb0)
                {
                    sourceBitIndex = 7 - sourceBitIndex; // Adjust source bit index
                    targetBitIndex = 7 - targetBitIndex; // Adjust target bit index
                }

                // Adjust target byte index for little-endian.
                if (endianness is Endianness.LittleEndian)
                    targetByteIndex = bytes.Length - 1 - targetByteIndex;

                // Set the current bit in the target byte.
                int mask = 1 << targetBitIndex; // Mask to clear the target bit
                int bit = (value[i] >> sourceBitIndex) & 1; // Extract the source bit
                bytes[targetByteIndex] = (byte)((bytes[targetByteIndex] & ~mask) | (bit << targetBitIndex)); // Set the target bit accordingly
            }
        }
    }
}