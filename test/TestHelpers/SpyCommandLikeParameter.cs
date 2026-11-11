namespace GcLib.UnitTests.Utilities;

/// <summary>
/// Test spy implementation of a command-style <see cref="GcParameter"/> that tracks parse invocations.
/// </summary>
/// <remarks>Used in tests to emulate a non-writable command parameter. Increments
/// <c>FromStringCallCount</c> when parsed and uses its name as the string representation.</remarks>
internal sealed class SpyCommandLikeParameter : GcParameter
{
    /// <summary>
    /// Gets the name of the parameter.
    /// </summary>
    public override string Name { get; }

    /// <summary>
    /// Gets the number of times <c>FromString</c> has been called.
    /// </summary>
    public int FromStringCallCount { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <c>SpyCommandLikeParameter</c> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the parameter.</param>
    public SpyCommandLikeParameter(string name) : base(name)
    {
        Name = name;
        Category = "Test";
        Description = string.Empty;
        IsReadable = true;
        IsWritable = false;
        Visibility = GcVisibility.Guru;
        Type = GcParameterType.Command;
        SelectedParameters = [];
        SelectingParameters = [];
    }

    /// <summary>
    /// Increments the <c>FromStringCallCount</c> each time it is called.
    /// </summary>
    /// <param name="valueString">The string representation of the parameter value.</param>
    public override void FromString(string valueString)
    {
        FromStringCallCount++;
    }

    /// <summary>
    /// Returns the name of the current instance.
    /// </summary>
    /// <returns>The value of the Name property.</returns>
    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Creates a new parameter instance with the same name as the current instance.
    /// </summary>
    /// <returns>A new <see cref="GcParameter"/> initialized with the current <c>Name</c> value.</returns>
    public override GcParameter Copy()
    {
        return new SpyCommandLikeParameter(Name);
    }
}