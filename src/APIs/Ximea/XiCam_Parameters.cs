using System;
using System.Collections.Generic;
using System.Globalization;
using System.Xml;
using xiApi.NET;

namespace GcLib;

public sealed partial class XiCam
{
    /// <summary>
    /// xiApi.NET-specific dictionary of parameters (needed for GetParam and SetParam methods in xiApi), containing parameter name as key and xiApi.NET-specific parameter string identifier as value.
    /// </summary>
    private Dictionary<string, string> _xiApiParameterList;

    /// <inheritdoc/>
    protected override GcDeviceParameterCollection ImportParameters()
    {
        // Initialize lists.
        var parameterList = new List<GcParameter>();
        var failedParameterList = new List<string>();
        _xiApiParameterList = [];

        // Retrieve device manifest xml.
        string deviceManifest = null;
        try
        {
            deviceManifest = _xiCam.GetParamString(PRM.DEVICE_MANIFEST); //  Available currently on xiQ USB3.0, xiB, xiT cameras.
        }
        catch (xiExc ex)
        {
            throw new NotImplementedException("Camera does not implement a device manifest xml!", ex);
        }

        // Load device manifest into xml document.
        var doc = new XmlDocument();
        doc.LoadXml(deviceManifest);

        XmlElement root = doc.DocumentElement;

        // Retrieve list of category nodes.
        XmlNodeList categoryNodes = root.SelectNodes("//*[name()='RegisterDescription']/*[name()='Group']");

        // Traverse category nodes.
        foreach (XmlNode categoryNode in categoryNodes)
        {
            // Get category name.
            string category = categoryNode.Attributes["Comment"].Value;

            // Retrieve list of parameter child nodes.
            XmlNodeList parameterNodes = categoryNode.SelectNodes("*[name()='Integer' or name()='Float' or name()='String' or name()='Enumeration' or name()='Boolean' or name()='Command']");

            // Traverse parameter nodes.
            foreach (XmlNode parameterNode in parameterNodes)
            {
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                // Get parameter name.
                string parameterName = string.Empty;
                if (parameterNode.SelectSingleNode("*[name()='pValue']") != null)
                    parameterName = parameterNode.SelectSingleNode("*[name()='pValue']").FirstChild.Value[0..^3]; // trim away "Reg" ending
                else parameterName = textInfo.ToTitleCase(parameterNode.SelectSingleNode("*[name()='DisplayName']").FirstChild.Value).Replace(" ", string.Empty); // or use display name (with capitalized words and without whitespaces)

                // Skip problematic parameters.
                if (parameterName == "BandwidthAvailable") // Measures available bandwidth each time it is read (while resetting camera)
                {
                    failedParameterList.Add(parameterName);
                    continue;
                }

                try
                {
                    // Get parameter description.
                    string description = string.Empty;
                    if (parameterNode.SelectSingleNode("*[name()='Description']") != null)
                        description = parameterNode.SelectSingleNode("*[name()='Description']").FirstChild.Value;

                    // Get parameter visibility.
                    string visibility = GcVisibility.Guru.ToString();
                    if (parameterNode.SelectSingleNode("*[name()='Visibility']") != null)
                        visibility = parameterNode.SelectSingleNode("*[name()='Visibility']").FirstChild.Value;

                    // Make these parameters invisible.
                    if (parameterName == "ApiContextList"                   // List of current parameters settings context - parameters with values. Used for offline processing.
                        || parameterName == "SensDefectsCorrListContent")   // Sets/Gets sensor defects list in special text format.
                    {
                        visibility = GcVisibility.Invisible.ToString();
                    }

                    // Ignore invisible?
                    //if (visibility == GcVisibility.Invisible.ToString())
                    //    continue;

                    // Get parameter accessibility (RO, RW or WO).
                    string accessMode = "RO";
                    if (parameterNode.SelectSingleNode("*[name()='ImposedAccessMode']") != null)
                        accessMode = parameterNode.SelectSingleNode("*[name()='ImposedAccessMode']").FirstChild.Value;

                    // Get parameter dependencies.
                    bool isSelector = false;
                    List<string> selectedParameters = null;
                    XmlNodeList selectedParameterNodes = parameterNode.SelectNodes("*[name()='pSelected']");
                    if (selectedParameterNodes.Count > 1)
                    {
                        isSelector = true;
                        selectedParameters = [];
                        foreach (XmlNode node in selectedParameterNodes)
                            selectedParameters.Add(node.FirstChild.Value);
                    }

                    // Get xiApi.NET-specific parameter name.
                    string xiApiParameter = parameterNode.SelectSingleNode("*[name()='Xiapi_Par']").FirstChild.Value;

                    // Add parameter based on type.
                    switch (parameterNode.Name)
                    {
                        case "Integer":

                            long integerValue = 0;
                            long integerMax = 0;
                            long integerMin = 0;
                            long integerIncrement = 0;

                            // Try to read feature value using xiApi.NET (as int32).
                            try
                            {
                                integerValue = _xiCam.GetParamInt(xiApiParameter);
                                integerMax = _xiCam.GetParamInt(xiApiParameter + PRMM.MAX);
                                integerMin = _xiCam.GetParamInt(xiApiParameter + PRMM.MIN);
                                integerIncrement = _xiCam.GetParamInt(xiApiParameter + PRMM.INC);
                            }
                            catch (xiExc)
                            {
                                failedParameterList.Add(parameterName);
                                continue;
                            }

                            parameterList.Add(new GcInteger(name: parameterName, category: category, description: description, unit: string.Empty,
                                                            value: integerValue, min: integerMin, max: integerMax, increment: integerIncrement, incrementMode: EIncMode.fixedIncrement, listOfValidValue: null,
                                                            isReadable: accessMode == "RO" || accessMode == "RW", isWritable: accessMode == "RW" || accessMode == "WO", visibility: Enum.Parse<GcVisibility>(visibility),
                                                            isSelector: isSelector, selectingParameters: null, selectedParameters: selectedParameters));

                            break;

                        case "Float":

                            // Try to read feature value using xiApi.NET.
                            double Value = 0;
                            double Max = 0;
                            double Min = 0;
                            double Increment = 0;
                            long Precision = 3;
                            try
                            {
                                Value = (double)Convert.ToDecimal(_xiCam.GetParamFloat(xiApiParameter));
                                Max = (double)Convert.ToDecimal(_xiCam.GetParamFloat(xiApiParameter + PRMM.MAX));
                                Min = (double)Convert.ToDecimal(_xiCam.GetParamFloat(xiApiParameter + PRMM.MIN));
                                Increment = (double)Convert.ToDecimal(_xiCam.GetParamFloat(xiApiParameter + PRMM.INC));
                                if (Increment > 0)
                                    Precision = (long)Math.Round(Math.Log10(1.0 / Increment));

                            }
                            catch (xiExc)
                            {
                                failedParameterList.Add(parameterName);
                                continue;
                            }

                            parameterList.Add(new GcFloat(name: parameterName, category: category, description: description, unit: string.Empty,
                                                          value: Value, min: Min, max: Max, increment: Increment,
                                                          displayPrecision: Precision, // needs further testing!
                                                          isReadable: accessMode == "RO" || accessMode == "RW", isWritable: accessMode == "RW" || accessMode == "WO", visibility: Enum.Parse<GcVisibility>(visibility),
                                                          isSelector: isSelector, selectingParameters: null, selectedParameters: selectedParameters));

                            break;

                        case "Enumeration":

                            // Retrieve enumeration entries.
                            var enumEntries = new List<GcEnumEntry>();
                            XmlNodeList enumEntryNodes = parameterNode.SelectNodes("*[name()='EnumEntry']");
                            foreach (XmlNode enumEntryNode in enumEntryNodes)
                            {
                                string symbolicName = string.Empty;
                                if (enumEntryNode.SelectSingleNode("*[name()='DisplayName']").FirstChild == null) // empty node?
                                    continue;
                                else symbolicName = enumEntryNode.SelectSingleNode("*[name()='DisplayName']").FirstChild.Value;

                                int intValue = Convert.ToInt32(enumEntryNode.SelectSingleNode("*[name()='Value']").FirstChild.Value);

                                enumEntries.Add(new GcEnumEntry(symbolicName, intValue));
                            }

                            if (enumEntries.Count == 0) // no entries
                            {
                                failedParameterList.Add(parameterName);
                                continue;
                            }

                            // Try to read feature value using xiApi.NET.                     
                            GcEnumEntry enumEntry;
                            int intEnumValue = 0;
                            try
                            {
                                intEnumValue = Convert.ToInt32(_xiCam.GetParamString(xiApiParameter));
                                enumEntry = enumEntries.Find(s => s.ValueInt == intEnumValue);
                            }
                            catch (xiExc)
                            {
                                failedParameterList.Add(parameterName);
                                continue;
                            }

                            parameterList.Add(new GcEnumeration(name: parameterName, category: category, description: description,
                                                                gcEnumEntry: enumEntry, gcEnumEntries: enumEntries,
                                                                isReadable: accessMode == "RO" || accessMode == "RW", isWritable: accessMode == "RW" || accessMode == "WO", visibility: Enum.Parse<GcVisibility>(visibility),
                                                                isSelector: isSelector, selectingParameters: null, selectedParameters: selectedParameters));

                            break;

                        case "Boolean":

                            // Try to read feature value using xiApi.NET.
                            bool booleanValue = false;
                            try
                            {
                                booleanValue = _xiCam.GetParamInt(xiApiParameter) == 1;
                            }
                            catch (xiExc)
                            {
                                failedParameterList.Add(parameterName);
                                continue;
                            }

                            parameterList.Add(new GcBoolean(name: parameterName, category: category, description: description,
                                                            value: booleanValue,
                                                            isReadable: accessMode == "RO" || accessMode == "RW", isWritable: accessMode == "RW" || accessMode == "WO", visibility: Enum.Parse<GcVisibility>(visibility),
                                                            isSelector: isSelector, selectingParameters: null, selectedParameters: selectedParameters));

                            break;

                        case "String":

                            // Try to read feature value using xiApi.NET.
                            string stringValue = string.Empty;
                            try
                            {
                                stringValue = _xiCam.GetParamString(xiApiParameter);
                            }
                            catch (xiExc)
                            {
                                failedParameterList.Add(parameterName);
                                continue;
                            }

                            parameterList.Add(new GcString(name: parameterName, category: category, description: description,
                                                           value: stringValue, maxLength: 100000, // enough?
                                                           isReadable: accessMode == "RO" || accessMode == "RW", isWritable: accessMode == "RW" || accessMode == "WO", visibility: Enum.Parse<GcVisibility>(visibility),
                                                           isSelector: isSelector, selectingParameters: null, selectedParameters: selectedParameters));

                            break;

                        case "Command":

                            // Try to read feature value using xiApi.NET.
                            int intCommandValue = 0;
                            try
                            {
                                _xiCam.GetParam(xiApiParameter, out intCommandValue);
                            }
                            catch (xiExc)
                            {
                                failedParameterList.Add(parameterName);
                                continue;
                            }

                            parameterList.Add(new GcCommand(name: parameterName,
                                                            category: category,
                                                            description: description,
                                                            method: () => _xiCam.SetParam(_xiApiParameterList[parameterName], intCommandValue),
                                                            isReadable: accessMode == "RO" || accessMode == "RW",
                                                            isWritable: accessMode == "RW" || accessMode == "WO",
                                                            visibility: Enum.Parse<GcVisibility>(visibility),
                                                            isSelector: isSelector,
                                                            selectingParameters: null,
                                                            selectedParameters: selectedParameters));

                            break;

                        default:
                            break;
                    }

                    // Add xiApi.NET-specific parameter name to list.
                    if (_xiApiParameterList.ContainsKey(parameterName) == false) // avoid duplicates
                        _xiApiParameterList.Add(parameterName, xiApiParameter);
                }
                catch (xiExc)
                {
                    // Failed due to exception in API.
                    failedParameterList.Add(parameterName);
                    //System.Diagnostics.Debug.WriteLine(ex.Message);
                }
                catch (Exception)
                {
                    failedParameterList.Add(parameterName);
                    //System.Diagnostics.Debug.WriteLine(ex.Message);
                }
            }
        }

        return new GcDeviceParameterCollection(this, parameterList, failedParameterList);
    }

    /// <inheritdoc/>
    protected override string GetParameterValue(string parameterName)
    {
        GcParameter parameter = Parameters[parameterName];
        string xiApiParameter = _xiApiParameterList[parameterName];

        return parameter.Type switch
        {
            GcParameterType.Integer => _xiCam.GetParamInt(xiApiParameter).ToString(),
            GcParameterType.Float => _xiCam.GetParamFloat(xiApiParameter).ToString(),
            GcParameterType.String => _xiCam.GetParamString(xiApiParameter).ToString(),
            GcParameterType.Boolean => (_xiCam.GetParamInt(xiApiParameter) == 1).ToString(),
            GcParameterType.Enumeration => (parameter as GcEnumeration).GetEntry(Convert.ToInt32(_xiCam.GetParamString(xiApiParameter))).ValueString,
            _ => null,
        };
    }

    /// <inheritdoc/>
    protected override void SetParameterValue(string parameterName, string parameterValue)
    {
        string xiApiParameter = _xiApiParameterList[parameterName];

        try
        {
            switch (Parameters[parameterName].Type)
            {
                case GcParameterType.Integer:
                    _xiCam.SetParam(xiApiParameter, Convert.ToInt32(parameterValue));
                    break;

                case GcParameterType.Float:
                    _xiCam.SetParam(xiApiParameter, Convert.ToSingle(parameterValue));
                    break;

                case GcParameterType.String:
                    _xiCam.SetParam(xiApiParameter, parameterValue);
                    break;

                case GcParameterType.Boolean:
                    _xiCam.SetParam(xiApiParameter, Convert.ToByte(parameterValue == bool.TrueString));
                    break;

                case GcParameterType.Enumeration:
                    _xiCam.SetParam(xiApiParameter, (int)(Parameters[parameterName] as GcEnumeration).GetEntryByName(parameterValue).ValueInt);
                    break;
            }
        }
        catch (xiExc)
        {
            // handle how? log?
            throw;
        }
    }

    /// <inheritdoc/>
    protected override void ExecuteParameterCommand(string parameterName)
    {
        (Parameters[parameterName] as GcCommand)?.Execute();
    }

    /// <inheritdoc/>
    protected override GcBoolean GetBoolean(string parameterName)
    {
        if (Parameters[parameterName] is GcBoolean gcBoolean)
        {
            // Retrieve current value from camera using API.
            string xiApiParameter = _xiApiParameterList[parameterName];
            int integerValue = _xiCam.GetParamInt(xiApiParameter);
            bool booleanValue = integerValue == 1;

            // Update parameter value.
            gcBoolean.Value = booleanValue;
            return gcBoolean;
        }
        else return new GcBoolean(parameterName);

    }

    /// <inheritdoc/>
    protected override GcCommand GetCommand(string parameterName)
    {
        if (Parameters[parameterName] is GcCommand gcCommand)
            return gcCommand;
        else return new GcCommand(parameterName);
    }

    /// <inheritdoc/>
    protected override GcEnumeration GetEnumeration(string parameterName)
    {
        if (Parameters[parameterName] is GcEnumeration gcEnumeration)
        {
            // Retrieve current value from camera using API.
            string xiApiParameter = _xiApiParameterList[parameterName];
            int intEnumValue = Convert.ToInt32(_xiCam.GetParamString(xiApiParameter));

            // Update parameter value.
            gcEnumeration.IntValue = intEnumValue;
            return gcEnumeration;
        }
        else return new GcEnumeration(parameterName);
    }

    /// <inheritdoc/>
    protected override GcFloat GetFloat(string parameterName)
    {
        if (Parameters[parameterName] is GcFloat gcFloat)
        {
            // Retrieve current value from camera using API.
            string xiApiParameter = _xiApiParameterList[parameterName];
            double value = (double)Convert.ToDecimal(_xiCam.GetParamFloat(xiApiParameter));

            // Update parameter value.
            gcFloat.Value = value;
            return gcFloat;
        }
        else return new GcFloat(parameterName);
    }

    /// <inheritdoc/>
    protected override GcInteger GetInteger(string parameterName)
    {
        if (Parameters[parameterName] is GcInteger gcInteger)
        {
            // Retrieve current value from camera using API.
            string xiApiParameter = _xiApiParameterList[parameterName];
            long integerValue = _xiCam.GetParamInt(xiApiParameter);

            // Update parameter value.
            gcInteger.Value = integerValue;
            return gcInteger;
        }
        return new GcInteger(parameterName);
    }

    /// <inheritdoc/>
    protected override GcString GetString(string parameterName)
    {
        if (Parameters[parameterName] is GcString gcString)
        {
            // Retrieve current value from camera using API.
            string xiApiParameter = _xiApiParameterList[parameterName];
            string stringValue = _xiCam.GetParamString(xiApiParameter);

            // Update parameter value.
            gcString.Value = stringValue;
            return gcString;
        }
        else return new GcString(parameterName);
    }
}