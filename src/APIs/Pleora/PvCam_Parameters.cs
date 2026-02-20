using System;
using System.Collections.Generic;
using PvDotNet;

namespace GcLib;

/// <summary>
/// Vendor-specific camera class providing an interface to eBUS SDK from Pleora Technologies.
/// </summary>
public sealed partial class PvCam
{
    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        var parameterList = new List<GcParameter>();
        var failedParameterList = new List<string>();

        foreach (PvGenParameter pvGenParameter in _pvDevice.Parameters)
        {
            // Only return available features.
            if (pvGenParameter.IsAvailable == false) // eliminate non-readable? (how to deal with IsPersistent and IsStreamable?)
            {
                failedParameterList.Add(pvGenParameter.Name);
                continue;
            }

            try
            {
                switch (pvGenParameter)
                {
                    case PvGenInteger pvGenInteger:
                        parameterList.Add(new GcInteger(name: pvGenInteger.Name,
                                                        category: pvGenInteger.Category["Root\\".Length..],
                                                        description: pvGenInteger.Description,
                                                        value: pvGenInteger.Value,
                                                        min: pvGenInteger.Min,
                                                        max: pvGenInteger.Max,
                                                        unit: pvGenInteger.Unit,
                                                        increment: pvGenInteger.Increment,
                                                        incrementMode: EIncMode.fixedIncrement,
                                                        listOfValidValue: null,
                                                        isReadable: pvGenInteger.IsReadable,
                                                        isWritable: pvGenInteger.IsWritable,
                                                        visibility: (GcVisibility)(int)pvGenInteger.Visibility,
                                                        isSelector: pvGenInteger.IsSelector,
                                                        selectedParameters: GetParameterNames(pvGenInteger.SelectedParameters),
                                                        selectingParameters: GetParameterNames(pvGenInteger.SelectingParameters)));
                        break;

                    case PvGenFloat pvGenFloat:
                        parameterList.Add(new GcFloat(name: pvGenFloat.Name,
                                                      category: pvGenFloat.Category["Root\\".Length..],
                                                      description: pvGenFloat.Description,
                                                      value: pvGenFloat.Value,
                                                      min: pvGenFloat.Min,
                                                      max: pvGenFloat.Max,
                                                      increment: 0.0,
                                                      displayPrecision: 3,
                                                      unit: pvGenFloat.Unit,
                                                      isReadable: pvGenFloat.IsReadable,
                                                      isWritable: pvGenFloat.IsWritable,
                                                      visibility: (GcVisibility)(int)pvGenFloat.Visibility,
                                                      isSelector: pvGenFloat.IsSelector,
                                                      selectedParameters: GetParameterNames(pvGenFloat.SelectedParameters),
                                                      selectingParameters: GetParameterNames(pvGenFloat.SelectingParameters)));
                        break;

                    case PvGenString pvGenString:
                        parameterList.Add(new GcString(name: pvGenString.Name,
                                                       category: pvGenString.Category["Root\\".Length..],
                                                       description: pvGenString.Description,
                                                       value: pvGenString.Value,
                                                       maxLength: pvGenString.MaxLength,
                                                       isReadable: pvGenString.IsReadable,
                                                       isWritable: pvGenString.IsWritable,
                                                       visibility: (GcVisibility)(int)pvGenString.Visibility,
                                                       isSelector: pvGenString.IsSelector,
                                                       selectedParameters: GetParameterNames(pvGenString.SelectedParameters),
                                                       selectingParameters: GetParameterNames(pvGenString.SelectingParameters)));
                        break;

                    case PvGenBoolean pvGenBoolean:
                        parameterList.Add(new GcBoolean(name: pvGenBoolean.Name,
                                                        category: pvGenBoolean.Category["Root\\".Length..],
                                                        description: pvGenBoolean.Description,
                                                        value: pvGenBoolean.Value,
                                                        isWritable: pvGenBoolean.IsWritable,
                                                        isReadable: pvGenBoolean.IsReadable,
                                                        visibility: (GcVisibility)(int)pvGenBoolean.Visibility,
                                                        isSelector: pvGenBoolean.IsSelector,
                                                        selectedParameters: GetParameterNames(pvGenBoolean.SelectedParameters),
                                                        selectingParameters: GetParameterNames(pvGenBoolean.SelectingParameters)));
                        break;

                    case PvGenEnum pvGenEnum:
                        parameterList.Add(new GcEnumeration(name: pvGenEnum.Name,
                                                            category: pvGenEnum.Category["Root\\".Length..],
                                                            description: pvGenEnum.Description,
                                                            gcEnumEntry: new GcEnumEntry(pvGenEnum.ValueString, pvGenEnum.ValueInt),
                                                            gcEnumEntries: ConvertEnumList(pvGenEnum),
                                                            isReadable: pvGenEnum.IsReadable,
                                                            isWritable: pvGenEnum.IsWritable,
                                                            visibility: (GcVisibility)(int)pvGenEnum.Visibility,
                                                            isSelector: pvGenEnum.IsSelector,
                                                            selectedParameters: GetParameterNames(pvGenEnum.SelectedParameters),
                                                            selectingParameters: GetParameterNames(pvGenEnum.SelectingParameters)));
                        break;

                    case PvGenCommand pvGenCommand:
                        parameterList.Add(new GcCommand(name: pvGenCommand.Name,
                                                        category: pvGenCommand.Category["Root\\".Length..],
                                                        description: pvGenCommand.Description,
                                                        method: () => pvGenCommand.Execute(),
                                                        isReadable: pvGenCommand.IsReadable,
                                                        isWritable: pvGenCommand.IsWritable,
                                                        visibility: (GcVisibility)(int)pvGenCommand.Visibility,
                                                        isSelector: pvGenCommand.IsSelector,
                                                        selectedParameters: GetParameterNames(pvGenCommand.SelectedParameters),
                                                        selectingParameters: GetParameterNames(pvGenCommand.SelectingParameters)));
                        break;
                }
            }
            catch (Exception)
            {
                // Some properties may render exceptions while attempting to read them. These can be captured here.
                failedParameterList.Add(pvGenParameter.Name);

                //throw;
                //System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    /// <inheritdoc/>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        PvGenParameter pvGenParameter = _pvDevice.Parameters.Get(parameterName);

        // Handle only writable, available, persistent parameters of non-command type.
        if (pvGenParameter == null || pvGenParameter.IsWritable == false || pvGenParameter.IsAvailable == false || pvGenParameter.IsPersistent == false || pvGenParameter.Type == PvGenType.Command)
            return;

        pvGenParameter.FromString(parameterValue);
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        PvGenParameter pvGenParameter = _pvDevice.Parameters.Get(parameterName);

        // Handle only readable and available parameters of non-command type.
        if (pvGenParameter == null || pvGenParameter.IsReadable == false || pvGenParameter.IsAvailable == false || pvGenParameter.Type == PvGenType.Command)
            return null;

        try
        {
            return pvGenParameter switch
            {
                PvGenInteger pvGenInteger => pvGenInteger.Value.ToString(),
                PvGenFloat pvGenFloat => pvGenFloat.Value.ToString(),
                PvGenString pvGenString => pvGenString.Value.ToString(),
                PvGenBoolean pvGenBoolean => pvGenBoolean.Value.ToString(),
                PvGenEnum pvGenEnum => pvGenEnum.ValueString,
                _ => string.Empty,
            };
        }
        catch (PvException)
        {
            throw;
        }
    }

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        PvGenParameter pvGenParameter = _pvDevice.Parameters.Get(parameterName);

        // Handle only available parameters of command type.
        if (pvGenParameter == null || pvGenParameter.IsAvailable == false || pvGenParameter.Type != PvGenType.Command)
            return;

        (pvGenParameter as PvGenCommand).Execute();

        // _pvDevice.Parameters.ExecuteCommand(parameterName); // alternative
    }

    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        if (Parameters[parameterName] is GcBoolean gcBoolean)
        {
            gcBoolean.Value = _pvDevice.Parameters.GetBooleanValue(parameterName);
            return gcBoolean;
        }
        return new GcBoolean(parameterName);
    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        if (Parameters[parameterName] is GcCommand gcCommand)
        {
            return gcCommand;
        }
        return new GcCommand(parameterName);
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        if (Parameters[parameterName] is GcEnumeration gcEnumeration)
        {
            gcEnumeration.StringValue = _pvDevice.Parameters.GetEnumValueAsString(parameterName);
            return gcEnumeration;
        }
        return new GcEnumeration(parameterName);
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        if (Parameters[parameterName] is GcFloat gcFloat)
        {
            gcFloat.Value = _pvDevice.Parameters.GetFloatValue(parameterName);
            return gcFloat;
        }
        return new GcFloat(parameterName);
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        if (Parameters[parameterName] is GcInteger gcInteger)
        {
            gcInteger.Value = _pvDevice.Parameters.GetIntegerValue(parameterName);
            return gcInteger;
        }
        return new GcInteger(parameterName);
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        if (Parameters[parameterName] is GcString gcString)
        {
            gcString.Value = _pvDevice.Parameters.GetStringValue(parameterName);
            return gcString;
        }
        return new GcString(parameterName);
    }

    /// <summary>
    /// Returns a list of parameter names from a list of PvGenParameter features.
    /// </summary>
    /// <param name="parameterList">List of parameters.</param>
    /// <returns>List of parameter names.</returns>
    private static List<string> GetParameterNames(List<PvGenParameter> parameterList)
    {
        var parameterNames = new List<string>();
        foreach (PvGenParameter pvGenParameter in parameterList)
            parameterNames.Add(pvGenParameter.Name);
        return parameterNames;
    }

    /// <summary>
    /// Converts list of available enumeration entries in PvGenEnum parameter to list of GcEnumEntry values.
    /// </summary>
    /// <param name="pvGenEnum">PvGenEnum enumeration parameter.</param>
    /// <returns>List of GcEnumEntry enumeration entries.</returns>
    private static List<GcEnumEntry> ConvertEnumList(PvGenEnum pvGenEnum)
    {
        var list = new List<GcEnumEntry>((int)pvGenEnum.EntriesCount);
        for (int i = 0; i < (int)pvGenEnum.EntriesCount; i++)
            list.Add(new GcEnumEntry(pvGenEnum.GetEntryByIndex(i).ValueString, pvGenEnum.GetEntryByIndex(i).ValueInt));
        return list;
    }
}