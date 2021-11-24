using System.Collections.Generic;
using System.Linq;
using Generator.Equals;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;

namespace Reductech.Utilities.SCLEditor;

[Equatable]
public abstract partial record ExampleInput(string Name, string Group)
{
    public abstract void SetInitialValue(ExampleChoiceData exampleChoiceData);

    public record ExampleChoice
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

            return new SetVariable<int>()
            {
                Variable = VariableName, Value = new IntConstant(value)
            };
        }
    }

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
                Variable = VariableName, Value = new StringConstant(value)
            };
        }
    }

    public record EnumValue(string Name, string Value);

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
                Variable = VariableName, Value = new StringConstant(value.Value)
            };
        }
    }

    public abstract partial record ExampleVariableInput(string Name, string Group)
        : ExampleInput(Name, Group)
    {
        public abstract IStep<Unit> GetStep(ExampleChoiceData exampleChoiceData);

        public VariableName VariableName => new(Name);
    }
}
