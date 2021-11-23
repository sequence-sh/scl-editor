using System.Collections.Generic;
using Reductech.EDR.Connectors.StructuredData;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Enums;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Steps;
using Reductech.EDR.Core.Util;

namespace Reductech.Utilities.SCLEditor;

public static class ExampleHelpers
{
    public static IEnumerable<ExampleData> AllExamples
    {
        get
        {
            yield return new ExampleData(
                "Convert CSV to Json",
                "convertcsvtojson",
                new ExampleComponent.Sequence(
                    "Sequence",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.Variable(DefaultInputs.CSVDelimiter),
                        new ExampleComponent.File(DefaultInputs.InputCSV)
                    }
                ),
                new ExampleOutput(
                    "Json",
                    "off",
                    new ToJsonArray()
                    {
                        Entities = new FromCSV()
                        {
                            Delimiter =
                                new GetVariable<StringStream>()
                                {
                                    Variable = DefaultInputs.CSVDelimiter.VariableName
                                },
                            Stream = new GetVariable<StringStream>()
                            {
                                Variable = new VariableName("inputcsv")
                            }
                        }
                    }
                )
            );

            yield return new ExampleData(
                "Convert Json to CSV",
                "convertjsontocsv",
                new ExampleComponent.Sequence(
                    "Sequence",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.Variable(DefaultInputs.CSVDelimiter),
                        new ExampleComponent.File(DefaultInputs.InputJson)
                    }
                ),
                new ExampleOutput(
                    "csv",
                    "off",
                    new ToCSV()
                    {
                        Delimiter =
                            new GetVariable<StringStream>()
                            {
                                Variable = DefaultInputs.CSVDelimiter.VariableName
                            },
                        Entities = new FromJsonArray()
                        {
                            Stream = new GetVariable<StringStream>()
                            {
                                Variable = new VariableName("inputjson")
                            }
                        }
                    }
                )
            );

            yield return new ExampleData(
                "Generate Schema",
                "generateschema",
                new ExampleComponent.Sequence(
                    "Sequence",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.Variable(
                            new ExampleInput.ExampleStringVariableInput(
                                "Schema Name",
                                "Schema",
                                "Schema"
                            )
                        ),
                        new ExampleComponent.Choice(
                            new ExampleInput.ExampleChoice(
                                "Input",
                                "Input",
                                new List<ExampleComponent>()
                                {
                                    new ExampleComponent.Sequence(
                                        "CSV",
                                        new List<ExampleComponent>()
                                        {
                                            new ExampleComponent.Variable(
                                                DefaultInputs.CSVDelimiter
                                            ),
                                            new ExampleComponent.File(DefaultInputs.InputCSV),
                                            new ExampleComponent.Constant(
                                                "ReadCSV",
                                                new SetVariable<Array<Entity>>()
                                                {
                                                    Variable = new VariableName("entities"),
                                                    Value = new FromCSV()
                                                    {
                                                        Delimiter =
                                                            new GetVariable<StringStream>()
                                                            {
                                                                Variable =
                                                                    DefaultInputs.CSVDelimiter
                                                                        .VariableName
                                                            },
                                                        Stream = new GetVariable<StringStream>()
                                                        {
                                                            Variable =
                                                                new VariableName("inputcsv")
                                                        }
                                                    }
                                                }
                                            )
                                        }
                                    ),
                                    new ExampleComponent.Sequence(
                                        "Json",
                                        new List<ExampleComponent>()
                                        {
                                            new ExampleComponent.File(DefaultInputs.InputJson),
                                            new ExampleComponent.Constant(
                                                "ReadJson",
                                                new SetVariable<Array<Entity>>()
                                                {
                                                    Variable = new VariableName("entities"),
                                                    Value = new FromJsonArray()
                                                    {
                                                        Stream = new GetVariable<StringStream>()
                                                        {
                                                            Variable =
                                                                new VariableName("inputjson")
                                                        }
                                                    }
                                                }
                                            )
                                        }
                                    )
                                }
                            )
                        )
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

            yield return new ExampleData(
                "Validate Schema",
                "validateschema",
                new ExampleComponent.Sequence(
                    "Sequence",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.File(DefaultInputs.SchemaJson),
                        new ExampleComponent.Choice(
                            new ExampleInput.ExampleChoice(
                                "Input",
                                "Input",
                                new List<ExampleComponent>()
                                {
                                    new ExampleComponent.Sequence(
                                        "CSV",
                                        new List<ExampleComponent>()
                                        {
                                            new ExampleComponent.Variable(
                                                DefaultInputs.CSVDelimiter
                                            ),
                                            new ExampleComponent.File(DefaultInputs.InputCSV),
                                            new ExampleComponent.Constant(
                                                "ReadCSV",
                                                new SetVariable<Array<Entity>>()
                                                {
                                                    Variable = new VariableName("entities"),
                                                    Value = new FromCSV()
                                                    {
                                                        Delimiter =
                                                            new GetVariable<StringStream>()
                                                            {
                                                                Variable =
                                                                    DefaultInputs.CSVDelimiter
                                                                        .VariableName
                                                            },
                                                        Stream = new GetVariable<StringStream>()
                                                        {
                                                            Variable =
                                                                new VariableName("inputcsv")
                                                        }
                                                    }
                                                }
                                            )
                                        }
                                    ),
                                    new ExampleComponent.Sequence(
                                        "Json",
                                        new List<ExampleComponent>()
                                        {
                                            new ExampleComponent.File(DefaultInputs.InputJson),
                                            new ExampleComponent.Constant(
                                                "ReadJson",
                                                new SetVariable<Array<Entity>>()
                                                {
                                                    Variable = new VariableName("entities"),
                                                    Value = new FromJsonArray()
                                                    {
                                                        Stream = new GetVariable<StringStream>()
                                                        {
                                                            Variable =
                                                                new VariableName("inputjson")
                                                        }
                                                    }
                                                }
                                            )
                                        }
                                    )
                                }
                            )
                        ),
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
                                        new EnumConstant<ErrorBehavior>(ErrorBehavior.Error)
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
