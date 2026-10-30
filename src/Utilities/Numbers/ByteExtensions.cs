using System;

namespace GcLib.Utilities.Numbers;

/// <summary>
/// Provides span-based extension methods for bit extraction and insertion.
/// </summary>
/// <remarks>This class includes methods that allow for the extraction and insertion of specific ranges of bits
/// within byte spans, accommodating different endianness and bit numbering conventions. It is useful in scenarios
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
        /// Represents the least significant bit (lsb) position in a binary value, where the rightmost bit is considered bit zero.
        /// </summary>
        Lsb0,
        /// <summary>
        /// Represents the most significant bit (msb) position in a binary value, where the leftmost bit is considered bit zero.
        /// </summary>
        Msb0
    }

    /// <summary>
    /// Represents the number of bits in a single byte. This value is always 8.
    /// </summary>
    public const int BitsPerByte = 8;

    /// <summary>
    /// Extracts a specified range of bits from a read-only source span into a caller-provided span.
    /// </summary>
    /// <remarks>The extracted bits are packed into the destination according to the specified endianness and
    /// bit numbering scheme. The source span is not modified. The destination must be large enough to hold the
    /// extracted bits; unused bits in the last written byte are cleared.</remarks>
    /// <param name="source">The read-only span from which bits are to be extracted.</param>
    /// <param name="start">The zero-based index, in bits, at which to begin extraction.</param>
    /// <param name="length">The number of bits to extract.</param>
    /// <param name="destination">The span that receives the extracted bits.</param>
    /// <param name="endianness">Specifies the byte order to use when extracting bits.</param>
    /// <param name="bitNumbering">Specifies the bit numbering scheme to use when extracting bits.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start index or length is negative, or if the specified range exceeds the bounds of the source span.</exception>
    /// <exception cref="ArgumentException">Thrown when the destination span is too small to hold the extracted bits.</exception>
    public static void GetBitRange(this ReadOnlySpan<byte> source, int start, int length, Span<byte> destination, Endianness endianness = Endianness.LittleEndian, BitNumbering bitNumbering = BitNumbering.Lsb0)
    {
        // Calculate the length of the source span in bits.
        int maxLength = source.Length * BitsPerByte;

        // Calculate the end index of the bit range.
        int end = start + length;

        // Perform range validations.
        if (start >= maxLength || start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), start, $"Start must non-negative and lesser than {maxLength}");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Length must be non-negative");
        if (end > maxLength)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Range length must be less than or equal to {maxLength}");

        // Calculate the required destination length.
        var (byteLength, remainderLength) = Math.DivRem(length, BitsPerByte);
        int resultLength = byteLength + (remainderLength == 0 ? 0 : 1);
        if (destination.Length < resultLength)
            throw new ArgumentException("Destination span is too small to hold the extracted bits.", nameof(destination));

        Span<byte> result = destination[..resultLength];
        result.Clear();

        // Iterate through each byte in the result span.
        for (int i = 0; i < result.Length; i++)
        {
            // Compute each of the 8 bits of the ith byte.
            // Stop if start index >= end index (rest of the bits in the current byte remain 0).
            for (int j = 0; j < BitsPerByte && start < end; j++)
            {
                // Calculate the source byte and bit indices.
                var (sourceByteIndex, sourceBitIndex) = Math.DivRem(start++, BitsPerByte);

                // Current bit index in the result byte.
                int resultBitIndex = j;

                // Adjust for bit numbering.
                if (bitNumbering is BitNumbering.Msb0)
                {
                    resultBitIndex = 7 - resultBitIndex;
                    sourceBitIndex = 7 - sourceBitIndex;
                }

                // Adjust for byte ordering.
                if (endianness is Endianness.LittleEndian)
                    sourceByteIndex = source.Length - 1 - sourceByteIndex;

                // Set the current bit in the result byte.
                result[i] |= (byte)(((source[sourceByteIndex] >> sourceBitIndex) & 1) << resultBitIndex);
            }
        }
    }

    /// <summary>
    /// Inserts bits from a read-only source span into a target span without allocating memory.
    /// </summary>
    /// <remarks>Only the specified bits are modified. If the source contains fewer than <paramref name="length"/>
    /// bits, only the available source bits are written. Bit offsets and byte order are relative to the target span.</remarks>
    /// <param name="target">The span in which bits will be set.</param>
    /// <param name="start">The zero-based index, in bits, at which to begin insertion.</param>
    /// <param name="length">The maximum number of bits to set.</param>
    /// <param name="value">The read-only span containing the bits to insert.</param>
    /// <param name="endianness">Specifies the byte order of the target span.</param>
    /// <param name="bitNumbering">Specifies the bit numbering scheme.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start index or length is negative, or the specified range exceeds the target span.</exception>
    public static void SetBitRange(this Span<byte> target, int start, int length, ReadOnlySpan<byte> value, Endianness endianness = Endianness.LittleEndian, BitNumbering bitNumbering = BitNumbering.Lsb0)
    {
        // Calculate the length of the target span in bits.
        int maxLength = target.Length * BitsPerByte;

        // Calculate the end index of the bit range.
        int end = start + length;

        // Perform range validations.
        if (start >= maxLength || start < 0)
            throw new ArgumentOutOfRangeException(nameof(start), start, $"Start must non-negative and lesser than {maxLength}");
        if (length < 0)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Length must be non-negative");
        if (end > maxLength)
            throw new ArgumentOutOfRangeException(nameof(length), length, $"Range length must be less than or equal to {maxLength}");

        // Iterate through the source bytes needed for the specified range.
        for (int i = 0; i < value.Length && start < end; i++)
        {
            // Compute each of the 8 bits of the ith byte.
            // Stop if start index >= end index (remaining target bits are unchanged).
            for (int j = 0; j < BitsPerByte && start < end; j++)
            {
                // Calculate the target byte and bit indices.
                var (targetByteIndex, targetBitIndex) = Math.DivRem(start++, BitsPerByte); // Note the increment(++) in start

                // Current bit index in the source byte.
                int sourceBitIndex = j;

                // Adjust for bit numbering.
                if (bitNumbering is BitNumbering.Msb0)
                {
                    sourceBitIndex = 7 - sourceBitIndex; // Adjust source bit index
                    targetBitIndex = 7 - targetBitIndex; // Adjust target bit index
                }

                // Adjust for byte ordering.
                if (endianness is Endianness.LittleEndian)
                    targetByteIndex = target.Length - 1 - targetByteIndex;

                // Set the current bit in the target byte.
                int mask = 1 << targetBitIndex; // Create a mask for the target bit position
                int bit = (value[i] >> sourceBitIndex) & 1; // Extract the bit from the source byte
                target[targetByteIndex] = (byte)((target[targetByteIndex] & ~mask) | (bit << targetBitIndex)); // Clear the target bit and set it to the new value
            }
        }
    }
}