using System.Collections.Generic;
using System.Linq;
using Generator.Equals;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;

namespace Reductech.Utilities.SCLEditor;

[Equatable]
public partial record ExampleData(
    string Name,
    string Url,
    ExampleComponent ExampleComponent,
    ExampleOutput ExampleOutput)
{
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
}
