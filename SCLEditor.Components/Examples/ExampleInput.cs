using Sequence.Core;
using Sequence.Core.Internal;
using Sequence.Core.Steps;
using Sequence.Core.Util;

namespace Sequence.SCLEditor.Components.Examples;

/// <summary>
/// The input to an example
/// </summary>
/// <param name="Name">The name of the input</param>
/// <param name="Group">Where to group the input control on screen</param>
[Equatable]
public abstract partial record ExampleInput(string Name, string Group)
{
    /// <summary>
    /// Sets the initial values for this input
    /// </summary>
    /// <param name="exampleChoiceData"></param>
    public abstract void SetInitialValue(ExampleChoiceData exampleChoiceData);

    /// <summary>
    /// Allows you to choose between different modes
    /// </summary>
    public record Mode
    (
        string Name,
        string Group,
        IReadOnlyList<ExampleComponent> Options) : ExampleInput(Name, Group)
    {
        /// <inheritdoc />
        public override void SetInitialValue(ExampleChoiceData exampleChoiceData)
        {
            exampleChoiceData.ChoiceValues[Name] = Options.First();
        }
    }

    /// <summary>
    /// Reads text from a file
    /// </summary>
    public record ExampleFileInput(
            string Name,
            string Group,
            string Language,
            string InitialValue)
        : ExampleInput(Name, Group)
    {
        /// <inheritdoc />
        public override void SetInitialValue(ExampleChoiceData exampleChoiceData)
        {
            exampleChoiceData.Editors[Name] = null!;
        }
    }

    /// <summary>
    /// An integer variable
    /// </summary>
    public record ExampleIntVariableInput(
        string Name,
        string Group,
        int InitialValue,
        int? Minimum,
        int? Maximum,
        int? Step) : ExampleVariableInput(Name, Group)
    {
        /// <inheritdoc />
        public override void SetInitialValue(ExampleChoiceData exampleChoiceData)
        {
            exampleChoiceData.IntValues[Name] = InitialValue;
        }

        /// <inheritdoc />
        public override IStep<Unit> GetStep(ExampleChoiceData exampleChoiceData)
        {
            var value = exampleChoiceData.IntValues[Name];

            return new SetVariable<SCLInt>()
            {
                Variable = VariableName, Value = new SCLConstant<SCLInt>(new SCLInt(value))
            };
        }
    }

    /// <summary>
    /// A string variable
    /// </summary>
    public record ExampleStringVariableInput(
        string Name,
        string Group,
        string InitialValue) : ExampleVariableInput(Name, Group)
    {
        /// <inheritdoc />
        public override void SetInitialValue(ExampleChoiceData exampleChoiceData)
        {
            exampleChoiceData.StringValues[Name] = InitialValue;
        }

        /// <inheritdoc />
        public override IStep<Unit> GetStep(ExampleChoiceData exampleChoiceData)
        {
            var value = exampleChoiceData.StringValues[Name];

            return new SetVariable<StringStream>()
            {
                Variable = VariableName, Value = new SCLConstant<StringStream>(value)
            };
        }
    }

    /// <summary>
    /// A possible value for an enum variable
    /// </summary>
    public record EnumValue(string Name, string Value);

    /// <summary>
    /// An enum variable
    /// </summary>
    [Equatable]
    public partial record ExampleEnumVariableInput(
        string Name,
        string Group,
        EnumValue InitialValue,
        [property: OrderedEquality] IReadOnlyList<EnumValue> PossibleValues) : ExampleVariableInput(
        Name,
        Group
    )
    {
        /// <inheritdoc />
        public override void SetInitialValue(ExampleChoiceData exampleChoiceData)
        {
            exampleChoiceData.EnumValues[Name] = InitialValue;
        }

        /// <inheritdoc />
        public override IStep<Unit> GetStep(ExampleChoiceData exampleChoiceData)
        {
            var value = exampleChoiceData.EnumValues[Name];

            return new SetVariable<StringStream>()
            {
                Variable = VariableName, Value = new SCLConstant<StringStream>(value.Value)
            };
        }
    }

    /// <summary>
    /// A variable
    /// </summary>
    public abstract record ExampleVariableInput(string Name, string Group)
        : ExampleInput(Name, Group)
    {
        /// <summary>
        /// Get the step
        /// </summary>
        public abstract IStep<Unit> GetStep(ExampleChoiceData exampleChoiceData);

        /// <summary>
        /// The name of the variable
        /// </summary>
        public VariableName VariableName => new(Name);
    }
}
