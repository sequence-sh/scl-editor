using System;
using System.Collections.Generic;
using System.Linq;
using Generator.Equals;
using Reductech.EDR.Core.Internal;

namespace Reductech.Utilities.SCLEditor;

[Equatable]
public abstract partial record ExampleInput(string Name, TextLocation? TextLocation)
{
    public record ExampleFileInput(
            string Name,
            string Language,
            string InitialValue,
            TextLocation TextLocation)
        : ExampleInput(Name, TextLocation)
    {
        public static ExampleFileInput Create(
            string fileName,
            TextLocation? textLocation,
            IReadOnlyList<ExampleInput> providedInputs)
        {
            var pi = providedInputs
                .OfType<ExampleFileInput>()
                .FirstOrDefault(x => x.Name.Equals(fileName, StringComparison.OrdinalIgnoreCase));

            if (pi is not null)
                return pi with { TextLocation = textLocation };

            string language;
            string initialValue;

            {
                language     = "";
                initialValue = "";
            }

            return new ExampleFileInput(fileName, language, initialValue, textLocation);
        }
    }

    public record ExampleIntVariableInput(
        string Name,
        TextLocation? TextLocation,
        int InitialValue,
        int? Minimum,
        int? Maximum,
        int? Step,
        BoundValue<int> BValue) : ExampleVariableInput(Name, TextLocation)
    {
        /// <inheritdoc />
        public override string GetValueSCL()
        {
            return BValue.Value.ToString();
        }

        /// <inheritdoc />
        public override ExampleVariableInput WithNewBoundValue()
        {
            return this with { BValue = new BoundValue<int>() { Value = InitialValue } };
        }
    }

    public record ExampleStringVariableInput(
        string Name,
        TextLocation? TextLocation,
        string InitialValue,
        BoundValue<string> BValue) : ExampleVariableInput(Name, TextLocation)
    {
        /// <inheritdoc />
        public override string GetValueSCL()
        {
            return $"'{BValue.Value}'"; //TODO escape string SCLParsing.UnescapeDoubleQuoted
        }

        /// <inheritdoc />
        public override ExampleVariableInput WithNewBoundValue()
        {
            return this with { BValue = new BoundValue<string>() { Value = InitialValue } };
        }
    }

    [Equatable]
    public partial record ExampleEnumVariableInput(
        string Name,
        TextLocation? TextLocation,
        string InitialValue,
        BoundValue<string> BValue,
        [property: OrderedEquality] IReadOnlyList<string> PossibleValues) : ExampleVariableInput(
        Name,
        TextLocation
    )
    {
        /// <inheritdoc />
        public override string GetValueSCL()
        {
            return $"'{BValue.Value}'"; //TODO escape string SCLParsing.UnescapeDoubleQuoted
        }

        /// <inheritdoc />
        public override ExampleVariableInput WithNewBoundValue()
        {
            return this with { BValue = new BoundValue<string>() { Value = InitialValue } };
        }
    }

    public abstract partial record ExampleVariableInput(
            string Name,
            TextLocation? TextLocation)
        : ExampleInput(Name, TextLocation)
    {
        public static ExampleVariableInput Create(
            string variableName,
            TextLocation? textLocation,
            IReadOnlyList<ExampleInput> providedInputs)
        {
            var pi = providedInputs
                .OfType<ExampleVariableInput>()
                .FirstOrDefault(
                    x => x.Name.Equals(variableName, StringComparison.OrdinalIgnoreCase)
                );

            if (pi is not null)
            {
                var newPi = pi with { TextLocation = textLocation };
                return newPi.WithNewBoundValue();
            }

            return new ExampleStringVariableInput(
                variableName,
                textLocation,
                "",
                new BoundValue<string>() { Value = "" }
            );
        }

        public abstract string GetValueSCL();
        public abstract ExampleVariableInput WithNewBoundValue();

        public string GetPrefixLine()
        {
            return $"- <{Name}> = {GetValueSCL()}";
        }
    }

    public class BoundValue<T>
    {
        public T Value { get; set; } = default;
    }
}
