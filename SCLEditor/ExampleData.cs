using System.Collections.Generic;
using System.Linq;
using Generator.Equals;
using Reductech.EDR.Connectors.FileSystem;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Steps;

namespace Reductech.Utilities.SCLEditor
{

[Equatable]
public partial record ExampleData(
    string Name,
    string Url,
    string Scl,
    ExampleOutput ExampleOutput,
    [property: OrderedEquality] IReadOnlyList<ExampleInput> Inputs)
{
    public static ExampleData Create(
        string name,
        string url,
        string scl,
        ExampleOutput exampleOutput,
        params ExampleInput[] inputs)
    {
        var fs = SCLParsing.TryParseStep(scl);

        var realInputs = GetInputs(fs.Value, inputs).ToList();

        var exampleData = new ExampleData(name, url, scl, exampleOutput, realInputs);
        return exampleData;
    }

    static IEnumerable<ExampleInput> GetInputs(
        IFreezableStep step,
        IReadOnlyList<ExampleInput> inputs)
    {
        if (step is CompoundFreezableStep cfs)
        {
            if (step.StepName == nameof(GetVariable<object>))
            {
                var vn = cfs.FreezableStepData.TryGetVariableName(
                    nameof(GetVariable<object>.Variable),
                    typeof(GetVariable<object>)
                );

                if (vn.IsSuccess)
                {
                    var input = ExampleInput.ExampleVariableInput.Create(
                        vn.Value.Name,
                        step.TextLocation,
                        inputs
                    );

                    yield return input;
                }
            }
            else if (step.StepName == nameof(SetVariable<object>))
            {
                var valueStep = cfs
                    .FreezableStepData
                    .TryGetStep(nameof(SetVariable<object>.Value), typeof(SetVariable<>));

                if (valueStep.IsSuccess)
                {
                    foreach (var exampleInput in GetInputs(valueStep.Value, inputs))
                    {
                        yield return exampleInput;
                    }
                }
            }

            else if (step.StepName == nameof(FileRead))
            {
                var pathStep = cfs
                    .FreezableStepData
                    .TryGetStep(nameof(FileRead.Path), typeof(FileRead));

                if (pathStep.IsSuccess && pathStep.Value is StringConstantFreezable scf)
                {
                    var efi = ExampleInput.ExampleFileInput.Create(
                        scf.Value.GetString(),
                        scf.TextLocation,
                        inputs
                    );

                    yield return efi;
                }
            }
            else
            {
                foreach (var fsp in cfs.FreezableStepData.StepProperties)
                {
                    if (fsp.Value is FreezableStepProperty.StepList stepList)
                    {
                        foreach (var freezableStep in stepList.List)
                        {
                            foreach (var exampleInput in GetInputs(freezableStep, inputs))
                            {
                                yield return exampleInput;
                            }
                        }
                    }
                    else
                    {
                        var childStep = fsp.Value.ConvertToStep();

                        foreach (var sclInput in GetInputs(childStep, inputs))
                            yield return sclInput;
                    }
                }
            }
        }
    }
}

}
