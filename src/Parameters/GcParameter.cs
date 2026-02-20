using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GcLib;

/// <summary>
/// Abstract base class for all parameters.
/// </summary>
public abstract class GcParameter : INotifyPropertyChanged
{
    #region Properties

    /// <summary>
    /// Parameter name.
    /// </summary>
    [Browsable(false)]
    public virtual string Name { get; }

    /// <summary>
    /// Display friendly name of parameter.
    /// </summary>
    [Browsable(true)]
    public string DisplayName => Name.Split('/', '\\').Last();

    /// <summary>
    /// Descriptive text associated with the parameter.
    /// </summary>
    [Browsable(false)]
    public string Description { get; protected set; }

    /// <summary>
    /// Category associated with the parameter.
    /// </summary>
    [Browsable(false)]
    public string Category { get; protected set; }

    /// <summary>
    /// <see langword="true"></see> if the parameter is implemented.
    /// </summary>
    [Browsable(false)]
    public bool IsImplemented { get; protected set; } = true;

    /// <summary>
    /// <see langword="true"></see> if the parameter is readable.
    /// </summary>
    [Browsable(false)]
    public bool IsReadable { get; protected set; }

    /// <summary>
    /// <see langword="true"></see> if the parameter is writable.
    /// </summary>
    [Browsable(false)]
    public bool IsWritable { get; protected set; }

    /// <summary>
    /// Parameter type.
    /// </summary>
    [Browsable(true)]
    public GcParameterType Type { get; protected set; }

    /// <summary>
    /// Parameter visibility.
    /// </summary>
    [Browsable(false)]
    public GcVisibility Visibility { get; protected set; }

    /// <summary>
    /// <see langword="true"></see> if changes to the parameter invokes changes in others.
    /// </summary>
    [Browsable(false)]
    public bool IsSelector { get; protected set; }

    /// <summary>
    /// Gets the names of parameters that are changed by this parameter.
    /// </summary>
    [Browsable(false)]
    public List<string> SelectedParameters { get; protected set; }

    /// <summary>
    /// Gets the names of parameters that changes this parameter.
    /// </summary>
    [Browsable(false)]
    public List<string> SelectingParameters { get; protected set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new parameter with the specified name.
    /// </summary>
    /// <param name="name">The name of the parameter. Cannot be null, empty, or contain any whitespace characters.</param>
    /// <exception cref="ArgumentException"></exception>
    protected GcParameter(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Parameter name cannot be empty!", name);

        if (name.Any(char.IsWhiteSpace))
            throw new ArgumentException("Parameter name cannot contain any whitespace characters!", name);
    }

    #endregion

    #region Events

    /// <summary>
    /// Event raised when a property of the class has been changed.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    /// Event-invoking method raising the PropertyChanged event when a property of the class has been changed. 
    /// </summary>
    /// <param name="propertyName">Name of the property that was changed.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Methods

    /// <summary>
    /// Test if the parameter is visible at the specified user visibility level.
    /// </summary>
    /// <param name="visibility">User visibility level.</param>
    /// <returns>True if parameter is visible.</returns>
    public bool IsVisible(GcVisibility visibility)
    {
        return Visibility <= visibility;
    }

    /// <summary>
    /// Set the parameter value from a string. 
    /// </summary>
    /// <param name="valueString">A string representing the new value to set the parameter to.</param>
    public abstract void FromString(string valueString);

    /// <summary>
    /// Get the parameter value as a string.
    /// </summary>
    /// <returns>String representation of parameter value.</returns>
    public abstract override string ToString();

    /// <summary>
    /// Creates a (deep) copy of the parameter, with the same properties but housed in a new object.
    /// </summary>
    /// <returns>Copy of the parameter.</returns>
    public abstract GcParameter Copy();

    /// <summary>
    /// Enforce changes to readability and writability settings of parameter.
    /// </summary>
    /// <param name="isReadable">New readability setting.</param>
    /// <param name="isWritable">New writability setting.</param>
    public void ImposeAccessMode(bool isReadable, bool isWritable)
    {
        if (isReadable != IsReadable)
        {
            IsReadable = isReadable;
            OnPropertyChanged(nameof(IsReadable));
        }

        if (isWritable != IsWritable)
        {
            IsWritable = isWritable;
            OnPropertyChanged(nameof(IsWritable));
        }
    }

    #endregion
}