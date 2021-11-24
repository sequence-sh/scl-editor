namespace Reductech.Utilities.SCLEditor;

/// <summary>
/// The Output of an example
/// </summary>
public record ExampleOutput(string? Language, string WordWrap, IStep<StringStream> FinalStep);

/// <summary>
/// Data about choices users have made regarding this example
/// </summary>
public class ExampleChoiceData
{
    /// <summary>
    /// Editors (used for file inputs)
    /// </summary>
    public readonly Dictionary<string, object> Editors = new();

    /// <summary>
    /// int values
    /// </summary>
    public readonly Dictionary<string, int> IntValues = new();

    /// <summary>
    /// String values
    /// </summary>
    public readonly Dictionary<string, string> StringValues = new();

    /// <summary>
    /// Enum values
    /// </summary>
    public readonly Dictionary<string, ExampleInput.EnumValue> EnumValues = new();

    /// <summary>
    /// Choice values
    /// </summary>
    public readonly Dictionary<string, ExampleComponent> ChoiceValues = new();

    /// <summary>
    /// Create a new Example Choice Data
    /// </summary>
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
