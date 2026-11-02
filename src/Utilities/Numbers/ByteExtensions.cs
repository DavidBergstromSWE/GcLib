using System;

namespace GcLib.Utilities.Numbers;

/// <summary>
/// Provides span-based helpers for extracting and inserting bit ranges.
/// </summary>
/// <remarks>
/// These helpers operate on contiguous spans and support explicit endianness and bit-numbering rules.
/// Endianness controls byte-index traversal, while bit numbering controls bit-index interpretation inside each byte.
/// </remarks>
public static class ByteExtensions
{
    /// <summary>
    /// Specifies byte traversal order for bit-range operations.
    /// </summary>
    /// <remarks>
    /// In these helpers, <see cref="LittleEndian"/> traverses bytes in forward index order.
    /// <see cref="BigEndian"/> traverses bytes in reverse index order across the addressed span.
    /// </remarks>
    public enum Endianness
    {
        /// <summary>
        /// Represents byte order where the least significant byte is stored first.
        /// </summary>
        LittleEndian,
        /// <summary>
        /// Represents byte order where the most significant byte is stored first.
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
    /// <remarks>
    /// Extracted bits are packed into <paramref name="destination"/> according to the specified endianness and bit numbering rules.
    /// The source span is not modified. Unused bits in the final written destination byte are cleared.
    /// </remarks>
    /// <param name="source">The read-only span from which bits are to be extracted.</param>
    /// <param name="start">The zero-based index, in bits, at which to begin extraction.</param>
    /// <param name="length">The number of bits to extract.</param>
    /// <param name="destination">The span that receives the extracted bits.</param>
    /// <param name="endianness">Specifies byte traversal order while extracting bits.</param>
    /// <param name="bitNumbering">Specifies the bit numbering scheme to use when extracting bits.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start index or length is negative, or if the specified range exceeds the bounds of the source span.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="endianness"/> parameter is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="bitNumbering"/> parameter is invalid.</exception>
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

        // Validate endianness parameter.
        if (endianness is not (Endianness.BigEndian or Endianness.LittleEndian))
            throw new ArgumentOutOfRangeException(nameof(endianness), endianness, "Unsupported endianness.");

        // Validate bit numbering parameter.
        if (bitNumbering is not (BitNumbering.Lsb0 or BitNumbering.Msb0))
            throw new ArgumentOutOfRangeException(nameof(bitNumbering), bitNumbering, "Unsupported bit numbering.");

        Span<byte> result = destination[..resultLength];
        result.Clear(); // Clear the destination span to ensure unused bits are set to 0.

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
                if (endianness is Endianness.BigEndian)
                    sourceByteIndex = source.Length - 1 - sourceByteIndex;

                // Set the current bit in the result byte.
                result[i] |= (byte)(((source[sourceByteIndex] >> sourceBitIndex) & 1) << resultBitIndex); // Extract the bit from the source byte and set it in the result byte at the correct position.
            }
        }
    }

    /// <summary>
    /// Inserts bits from a read-only source span into a target span without allocating memory.
    /// </summary>
    /// <remarks>
    /// Only the specified target range is modified. If <paramref name="value"/> contains fewer than <paramref name="length"/> bits,
    /// only available bits are written. Bit offsets and endianness are interpreted relative to <paramref name="target"/>.
    /// </remarks>
    /// <param name="target">The span in which bits will be set.</param>
    /// <param name="start">The zero-based index, in bits, at which to begin insertion.</param>
    /// <param name="length">The maximum number of bits to set.</param>
    /// <param name="value">The read-only span containing the bits to insert.</param>
    /// <param name="endianness">Specifies byte traversal order while writing bits into the target span.</param>
    /// <param name="bitNumbering">Specifies the bit numbering scheme.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start index or length is negative, or the specified range exceeds the target span.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="endianness"/> parameter is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="bitNumbering"/> parameter is invalid.</exception>
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

        // Validate endianness parameter.
        if (endianness is not (Endianness.BigEndian or Endianness.LittleEndian))
            throw new ArgumentOutOfRangeException(nameof(endianness), endianness, "Unsupported endianness.");

        // Validate bit numbering parameter.
        if (bitNumbering is not (BitNumbering.Lsb0 or BitNumbering.Msb0))
            throw new ArgumentOutOfRangeException(nameof(bitNumbering), bitNumbering, "Unsupported bit numbering.");

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
                if (endianness is Endianness.BigEndian)
                    targetByteIndex = target.Length - 1 - targetByteIndex;

                // Set the current bit in the target byte.
                int mask = 1 << targetBitIndex; // Create a mask for the target bit position
                int bit = (value[i] >> sourceBitIndex) & 1; // Extract the bit from the source byte
                target[targetByteIndex] = (byte)((target[targetByteIndex] & ~mask) | (bit << targetBitIndex)); // Clear the target bit and set it to the new value
            }
        }
    }
}