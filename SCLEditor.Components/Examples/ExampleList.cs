using Sequence.Connectors.StructuredData;
using Sequence.Core;
using Sequence.Core.Enums;
using Sequence.Core.Internal;
using Sequence.Core.Steps;
using Sequence.Core.Util;

namespace Sequence.SCLEditor.Components.Examples;

/// <summary>
/// List of SCL examples
/// </summary>
public static class ExampleList
{
    /// <summary>
    /// All scl examples
    /// </summary>
    public static IEnumerable<ExampleTemplate> AllExamples
    {
        get
        {
            yield return new ExampleTemplate(
                "Convert Data",
                "convertdata",
                new ExampleComponent.Sequence(
                    "Sequence",
                    new List<ExampleComponent>()
                    {
                        ExampleDefaults.InputEntities, ExampleDefaults.OutputEntities,
                    }
                ),
                new ExampleOutput(
                    null,
                    "off",
                    new GetVariable<StringStream>() { Variable = new VariableName("data") }
                )
            );

            yield return new ExampleTemplate(
                "Generate Schema",
                "generateschema",
                new ExampleComponent.Sequence(
                    "Sequence",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.Variable(
                            new ExampleInput.ExampleStringVariableInput(
                                "Schema Name",
                                "input",
                                "Schema"
                            )
                        ),
                        ExampleDefaults.InputEntities
                    }
                ),
                new ExampleOutput(
                    "json",
                    "off",
                    new ToJson
                    {
                        Entity = new SchemaCreateCoerced
                        {
                            SchemaName =
                                new GetVariable<StringStream>
                                {
                                    Variable = new VariableName("Schema Name")
                                },
                            AllowExtraProperties = new SCLConstant<SCLBool>(SCLBool.False),
                            Entities = new GetVariable<Array<Entity>>
                            {
                                Variable = new VariableName("entities")
                            }
                        },
                        FormatOutput = new SCLConstant<SCLBool>(SCLBool.True)
                    }
                )
            );

            yield return new ExampleTemplate(
                "Validate Schema",
                "validateschema",
                new ExampleComponent.Sequence(
                    "Sequence",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.File(ExampleDefaults.SchemaJson),
                        ExampleDefaults.InputEntities,
                        new ExampleComponent.Constant(
                            "Validate Entities",
                            new ForEach<Entity>()
                            {
                                Array = new Validate()
                                {
                                    EntityStream =
                                        new GetVariable<Array<Entity>>()
                                        {
                                            Variable = new VariableName("entities")
                                        },
                                    Schema = new FromJson()
                                    {
                                        Stream = new GetVariable<StringStream>()
                                        {
                                            Variable = new VariableName("schemajson")
                                        }
                                    },
                                    ErrorBehavior = new SCLConstant<SCLEnum<ErrorBehavior>>(
                                        new SCLEnum<ErrorBehavior>(ErrorBehavior.Fail)
                                    )
                                },
                                Action = new LambdaFunction<Entity, Unit>(
                                    null,
                                    new DoNothing()
                                )
                            }
                        )
                    }
                ),
                new ExampleOutput(
                    null,
                    "on",
                    new SCLConstant<StringStream>("Validation Successful")
                )
            );
        }
    }
}
