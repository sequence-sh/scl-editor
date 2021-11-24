namespace Reductech.Utilities.SCLEditor;

/// <summary>
/// A component used in an example
/// </summary>
/// <param name="Name"></param>
[Equatable]
public abstract partial record ExampleComponent(string Name)
{
    /// <summary>
    /// Get the example sequence
    /// </summary>
    public abstract IStep<Unit> GetSequence(ExampleChoiceData choiceData);

    /// <summary>
    /// Get the example inputs
    /// </summary>
    public abstract IEnumerable<ExampleInput> GetInputs(ExampleChoiceData choiceData);

    /// <summary>
    /// Get all example inputs
    /// </summary>
    public abstract IEnumerable<ExampleInput> GetAllInputs();

    /// <summary>
    /// A choice of different example components
    /// </summary>
    [Equatable]
    public partial record Choice(ExampleInput.Mode Mode) : ExampleComponent(Mode.Name)
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
            yield return Mode;

            foreach (var chosenComponentInput in GetChoice(choiceData).GetInputs(choiceData))
            {
                yield return chosenComponentInput;
            }
        }

        /// <inheritdoc />
        public override IEnumerable<ExampleInput> GetAllInputs()
        {
            yield return Mode;

            foreach (var option in Mode.Options)
            foreach (var input in option.GetAllInputs())
                yield return input;
        }
    }

    /// <summary>
    /// Constant SCL
    /// </summary>
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

    /// <summary>
    /// A variable input
    /// </summary>
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

    /// <summary>
    /// A file input component
    /// </summary>
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

        /// <summary>
        /// The name of the variable
        /// </summary>
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

    /// <summary>
    /// A Sequence of Steps
    /// </summary>
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
                    .ToList()
            };
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
