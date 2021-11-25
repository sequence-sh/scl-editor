using Reductech.EDR.Core.Enums;

namespace Reductech.Utilities.SCLEditor;

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
                            AllowExtraProperties = new BoolConstant(false),
                            Entities = new GetVariable<Array<Entity>>
                            {
                                Variable = new VariableName("entities")
                            }
                        },
                        FormatOutput = new BoolConstant(true)
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
                                    ErrorBehavior =
                                        new EnumConstant<ErrorBehavior>(ErrorBehavior.Fail)
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
                    new StringConstant("Validation Successful")
                )
            );
        }
    }
}
