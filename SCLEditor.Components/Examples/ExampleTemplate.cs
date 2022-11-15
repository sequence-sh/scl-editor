using Sequence.Core;
using Sequence.Core.Internal;
using Sequence.Core.Steps;
using Sequence.Core.Util;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// A dynamic scl example
/// </summary>
[Equatable]
public partial record ExampleTemplate(
    string Name,
    string Url,
    ExampleComponent ExampleComponent,
    ExampleOutput ExampleOutput)
{
    /// <summary>
    /// Get the sequence
    /// </summary>
    /// <param name="choiceData"></param>
    /// <returns></returns>
    public CompoundStep<StringStream> GetSequence(ExampleChoiceData choiceData)
    {
        var initialSequence = ExampleComponent.GetSequence(choiceData);
        var finalSequence   = ExampleOutput.FinalStep;

        var fullSequence = new Sequence<StringStream>()
        {
            InitialSteps = Flatten(initialSequence).ToList(), FinalStep = finalSequence
        };

        return fullSequence;
    }

    private static IEnumerable<IStep<Unit>> Flatten(IStep<Unit> step)
    {
        if (step is Sequence<Unit> sequence)
        {
            foreach (var initial in sequence.InitialSteps)
            {
                foreach (var step1 in Flatten(initial))
                {
                    if (step1 is not DoNothing)
                        yield return step1;
                }
            }

            if (sequence.FinalStep is not DoNothing)
                yield return sequence.FinalStep;
        }
        else if (step is DoNothing) { }
        else
        {
            yield return step;
        }
    }
}
