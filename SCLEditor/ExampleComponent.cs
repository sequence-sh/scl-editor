using System.Collections.Generic;
using System.Linq;
using Generator.Equals;
using Reductech.EDR.Connectors.FileSystem;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;

namespace Reductech.Utilities.SCLEditor;

[Equatable]
public abstract partial record ExampleComponent(string Name)
{
    public abstract IStep<Unit> GetSequence(ExampleChoiceData choiceData);

    public abstract IEnumerable<ExampleInput> GetInputs(ExampleChoiceData choiceData);

    public abstract IEnumerable<ExampleInput> GetAllInputs();

    [Equatable]
    public partial record Choice(ExampleInput.ExampleChoice ExampleChoice) : ExampleComponent(
        ExampleChoice.Name
    )
    {
        private ExampleComponent GetChoice(ExampleChoiceData exampleChoiceData) =>
            exampleChoiceData.ChoiceValues[Name];

        /// <inheritdoc />
        public override IStep<Unit> GetSequence(ExampleChoiceData choiceData)
        {
            return GetChoice(choiceData).GetSequence(choiceData);
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetInputs(ExampleChoiceData choiceData)
        {
            yield return ExampleChoice;

            foreach (var chosenComponentInput in GetChoice(choiceData).GetInputs(choiceData))
            {
                yield return chosenComponentInput;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetAllInputs()
        {
            yield return ExampleChoice;

            foreach (var option in ExampleChoice.Options)
            foreach (var input in option.GetAllInputs())
                yield return input;
        }
    }

    [Equatable]
    public partial record Constant(string Name, IStep<Unit> Step) : ExampleComponent(Name)
    {
        /// <inheritdoc />
        public override IStep<Unit> GetSequence(ExampleChoiceData choiceData)
        {
            return Step;
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetInputs(ExampleChoiceData choiceData)
        {
            yield break;
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetAllInputs()
        {
            yield break;
        }
    }

    [Equatable]
    public partial record Variable
        (ExampleInput.ExampleVariableInput VariableInput) : ExampleComponent(VariableInput.Name)
    {
        /// <inheritdoc />
        public override IStep<Unit> GetSequence(ExampleChoiceData choiceData)
        {
            return VariableInput.GetStep(choiceData);
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetInputs(ExampleChoiceData choiceData)
        {
            yield return VariableInput;
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetAllInputs()
        {
            yield return VariableInput;
        }
    }

    [Equatable]
    public partial record File
        (ExampleInput.ExampleFileInput FileInput) : ExampleComponent(FileInput.Name)
    {
        /// <inheritdoc />
        public override IStep<Unit> GetSequence(ExampleChoiceData choiceData)
        {
            var step = new SetVariable<StringStream>()
            {
                Variable = VariableName,
                Value    = new FileRead() { Path = new StringConstant(Name) }
            };

            return step;
        }

        public VariableName VariableName
        {
            get
            {
                var name = new string(FileInput.Name.Where(char.IsLetterOrDigit).ToArray());
                return new VariableName(name);
            }
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetInputs(ExampleChoiceData choiceData)
        {
            yield return FileInput;
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetAllInputs()
        {
            yield return FileInput;
        }
    }

    [Equatable]
    public partial record Sequence(
        string Name,
        IReadOnlyList<ExampleComponent> Components) : ExampleComponent(Name)
    {
        /// <inheritdoc />
        public override IStep<Unit> GetSequence(ExampleChoiceData choiceData)
        {
            return new Sequence<Unit>()
            {
                FinalStep = new DoNothing(),
                InitialSteps = Components.Select(x => x.GetSequence(choiceData))
                    .SelectMany(Flatten)
                    .ToList()
            };
        }

        public IEnumerable<IStep<Unit>> Flatten(IStep<Unit> step)
        {
            if (step is Sequence<Unit> sequence)
            {
                foreach (var initial in sequence.InitialSteps)
                {
                    yield return initial;
                }

                yield return sequence.FinalStep;
            }
            else
            {
                yield return step;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetInputs(ExampleChoiceData choiceData)
        {
            return Components.SelectMany(x => x.GetInputs(choiceData));
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetAllInputs()
        {
            return Components.SelectMany(x => x.GetAllInputs());
        }
    }
}
