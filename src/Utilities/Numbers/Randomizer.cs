using System;
using System.Numerics;

namespace GcLib.Utilities.Numbers;

/// <summary>
/// Randomizer, mainly for random number generation but also provides randomized booleans and item retrievals from generic collections.
/// </summary>
public class Randomizer
{
    #region Fields

    /// <summary>
    /// Random number generator.
    /// </summary>
    readonly Random _rd;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes new instance of class using default seed value.
    /// </summary>
    public Randomizer()
    {
        _rd = new Random();
    }

    /// <summary>
    /// Initializes new instance of class using specified seed value.
    /// </summary>
    /// <param name="seed">
    /// A number used to calculate a starting value for the pseudo-random number sequence. If a negative number
    /// is specified, the absolute value of the number is used.
    /// </param>
    public Randomizer(int seed)
    {
        _rd = new Random(Seed: seed);
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Returns a random number of type <typeparamref name="T"/>, within limits specified.
    /// </summary>
    /// <typeparam name="T">Number type.</typeparam>
    /// <param name="Min">Minimum value.</param>
    /// <param name="Max">Maximum value.</param>
    /// <returns>Random number within the supplied bounds.</returns>
    /// <remarks>Integer ranges include both bounds. Non-finite floating-point bounds are not supported.</remarks>
    public T Next<T>(T Min, T Max) where T : INumber<T>
    {
        if (Max <= Min)
            throw new ArgumentOutOfRangeException(nameof(Max), "Maximum must be larger than minimum!");

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 => (T)Convert.ChangeType(Next(Min: Convert.ToInt32(Min), Max: Convert.ToInt32(Max)), typeof(T)),
            TypeCode.UInt32 => (T)Convert.ChangeType(Next(Min: Convert.ToInt64(Min), Max: Convert.ToInt64(Max)), typeof(T)),
            TypeCode.Int64 => (T)Convert.ChangeType(Next(Min: Convert.ToInt64(Min), Max: Convert.ToInt64(Max)), typeof(T)),
            TypeCode.UInt64 => (T)Convert.ChangeType(Next(Min: Convert.ToUInt64(Min), Max: Convert.ToUInt64(Max)), typeof(T)),
            TypeCode.Single => (T)Convert.ChangeType(Next(Min: Convert.ToSingle(Min), Max: Convert.ToSingle(Max)), typeof(T)),
            TypeCode.Double => (T)Convert.ChangeType(Next(Min: Convert.ToDouble(Min), Max: Convert.ToDouble(Max)), typeof(T)),
            TypeCode.Decimal => (T)Convert.ChangeType(Next(Min: Convert.ToDecimal(Min), Max: Convert.ToDecimal(Max)), typeof(T)),
            _ => throw new InvalidOperationException($"Type {nameof(T)} is not supported."),
        };
    }

    /// <summary>
    /// Returns a random non-negative number of type <typeparamref name="T"/>, which is less than or equal to specified maximum.
    /// </summary>
    /// <typeparam name="T">Number type.</typeparam>
    /// <param name="Max">Maximum value.</param>
    /// <returns>Random number.</returns>
    public T Next<T>(T Max) where T : INumber<T>
    {
        return Next(T.Zero, Max);
    }

    /// <summary>
    /// Retrieves random item from a collection.
    /// </summary>
    /// <typeparam name="T">Type of items.</typeparam>
    /// <param name="collection">Non-empty collection of items.</param>
    /// <returns>Random item.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="collection"/> is empty.</exception>
    public T NextItem<T>(T[] collection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        if (collection.Length == 0)
            throw new ArgumentException("Collection must contain at least one item.", nameof(collection));

        int index = _rd.Next(collection.Length);
        return collection[index];
    }

    /// <summary>
    /// Returns random boolean value.
    /// </summary>
    /// <returns><see langword="true"/> or <see langword="false"/>.</returns>
    public bool NextBoolean()
    {
        return _rd.Next(0, 2) == 1; // Note: System.Random.Next is exclusive on the upper bound.
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Returns a random integer, within limits specified.
    /// </summary>
    /// <param name="Min">Minimum value.</param>
    /// <param name="Max">Maximum value.</param>
    /// <returns>Random integer.</returns>
    private int Next(int Min, int Max)
    {
        return (int)Next((long)Min, Max);
    }

    /// <summary>
    /// Returns a random integer, within limits specified.
    /// </summary>
    /// <param name="Min">Minimum value.</param>
    /// <param name="Max">Maximum value.</param>
    /// <returns>Random integer.</returns>
    private long Next(long Min, long Max)
    {
        const ulong signBit = 0x8000000000000000UL;
        ulong orderedMin = unchecked((ulong)Min) ^ signBit;
        ulong orderedMax = unchecked((ulong)Max) ^ signBit;
        ulong range = unchecked(orderedMax - orderedMin + 1);
        ulong offset = range == 0 ? NextUInt64() : NextUInt64(range);
        ulong orderedResult = unchecked(orderedMin + offset);

        return unchecked((long)(orderedResult ^ signBit));
    }

    /// <summary>
    /// Returns a random integer, within limits specified.
    /// </summary>
    /// <param name="Min">Minimum value.</param>
    /// <param name="Max">Maximum value.</param>
    /// <returns>Random integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private ulong Next(ulong Min, ulong Max)
    {
        ulong range = unchecked(Max - Min + 1);
        ulong offset = range == 0 ? NextUInt64() : NextUInt64(range);
        return unchecked(Min + offset);
    }

    /// <summary>
    /// Returns a random floating-point value within the specified limits.
    /// </summary>
    private float Next(float Min, float Max)
    {
        return (float)Next((double)Min, Max);
    }

    /// <summary>
    /// Returns a random double within the specified limits.
    /// </summary>
    private double Next(double Min, double Max)
    {
        if (!double.IsFinite(Min) || !double.IsFinite(Max))
            throw new ArgumentOutOfRangeException(nameof(Max), "Bounds must be finite.");

        double fraction = _rd.NextDouble();
        double value = Min < 0 && Max > 0
            ? Min * (1 - fraction) + Max * fraction
            : Min + (Max - Min) * fraction;

        return Math.Clamp(value, Min, Max);
    }

    /// <summary>
    /// Returns a random decimal within the specified limits.
    /// </summary>
    private decimal Next(decimal Min, decimal Max)
    {
        decimal fraction = (decimal)_rd.NextDouble();
        decimal value = Min < 0 && Max > 0
            ? Min * (1 - fraction) + Max * fraction
            : Min + (Max - Min) * fraction;

        return Math.Clamp(value, Min, Max);
    }

    /// <summary>
    /// Generates a random 64-bit unsigned integer.
    /// </summary>
    /// <returns>A randomly generated 64-bit unsigned integer.</returns>
    private ulong NextUInt64()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        _rd.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes);
    }

    /// <summary>
    /// Returns a uniformly distributed unsigned 64-bit integer within the specified range.
    /// </summary>
    /// <param name="exclusiveUpperBound">The exclusive upper bound of the range. Must be greater than zero.</param>
    /// <returns>A value greater than or equal to zero and less than <paramref name="exclusiveUpperBound"/>.</returns>
    private ulong NextUInt64(ulong exclusiveUpperBound)
    {
        ulong threshold = unchecked(0UL - exclusiveUpperBound) % exclusiveUpperBound;
        ulong value;
        do
        {
            value = NextUInt64();
        }
        while (value < threshold);

        return value % exclusiveUpperBound;
    }

    #endregion
}