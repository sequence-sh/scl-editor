using System.Collections.Generic;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;

namespace Reductech.Utilities.SCLEditor;

public record ExampleOutput(string? Language, string WordWrap, IStep<StringStream> FinalStep);

/// <summary>
/// Data about choices users have made regarding this example
/// </summary>
public class ExampleChoiceData
{
    public readonly Dictionary<string, object> Editors = new();
    public readonly Dictionary<string, int> IntValues = new();
    public readonly Dictionary<string, string> StringValues = new();
    public readonly Dictionary<string, ExampleInput.EnumValue> EnumValues = new();

    public readonly Dictionary<string, ExampleComponent> ChoiceValues = new();

    public static ExampleChoiceData Create(ExampleComponent component)
    {
        var data = new ExampleChoiceData();

        foreach (var componentInput in component.GetAllInputs())
        {
            componentInput.SetInitialValue(data);
        }

        return data;
    }
}
