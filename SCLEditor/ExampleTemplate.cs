using System.Collections.Generic;
using System.Linq;
using Generator.Equals;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;

namespace Reductech.Utilities.SCLEditor;

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
                    yield return step1;
                }
            }

            yield return sequence.FinalStep;
        }
        else
        {
            yield return step;
        }
    }
}
