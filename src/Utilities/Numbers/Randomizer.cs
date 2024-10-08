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
    /// <returns>Random number.</returns>
    public T Next<T>(T Min, T Max) where T : INumber<T>
    {
        if (Max <= Min)
            throw new ArgumentOutOfRangeException(nameof(Max), "Maximum must be larger than minimum!");

        return Type.GetTypeCode(typeof(T)) switch
        {
            TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or TypeCode.Int32 => (T)Convert.ChangeType(Next(Min: Convert.ToInt32(Min), Max: Convert.ToInt32(Max)), typeof(T)),
            TypeCode.UInt32 or TypeCode.Int64 => (T)Convert.ChangeType(Next(Min: Convert.ToInt64(Min), Max: Convert.ToInt64(Max)), typeof(T)),
            TypeCode.UInt64 => (T)Convert.ChangeType(Next(Min: Convert.ToUInt64(Min), Max: Convert.ToUInt64(Max)), typeof(T)),
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal => (T)Convert.ChangeType(Next(Min: Convert.ToDecimal(Min), Max: Convert.ToDecimal(Max)), typeof(T)),
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
    /// <param name="collection">Collection of items.</param>
    /// <returns>Random item.</returns>
    public T NextItem<T>(T[] collection)
    {
        int index = _rd.Next(0, collection.Length);
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
        return _rd.Next(Min, Max + 1); // Note: System.Random.Next is exclusive on the upper bound.
    }

    /// <summary>
    /// Returns a random integer, within limits specified.
    /// </summary>
    /// <param name="Min">Minimum value.</param>
    /// <param name="Max">Maximum value.</param>
    /// <returns>Random integer.</returns>
    private long Next(long Min, long Max)
    {
        return _rd.NextInt64(Min, Max + 1); // Note: System.Random.Next is exclusive on the upper bound.
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
        return (ulong)(_rd.NextDouble() * (Max - Min) + Min);
    }

    /// <summary>
    /// Returns a random decimal value, within limits specified.
    /// </summary>
    /// <param name="Min">Minimum value.</param>
    /// <param name="Max">Maximum value.</param>
    /// <returns>Random integer.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    private decimal Next(decimal Min, decimal Max)
    {
        return (decimal)_rd.NextDouble() * (Max - Min) + Min;
    }

    #endregion
}