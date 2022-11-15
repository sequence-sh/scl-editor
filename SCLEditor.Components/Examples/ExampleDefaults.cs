using System.Text.Json;
using Json.Schema;
using Sequence.Connectors.StructuredData;
using Sequence.Core;
using Sequence.Core.Internal;
using Sequence.Core.Steps;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// Default values for components commonly used in examples
/// </summary>
public static class ExampleDefaults
{
    private static readonly ExampleInput.ExampleFileInput InputJson
        = new(
            "input.json",
            "Json",
            "json",
            @"[{
  ""stringProp"": ""hello"",
  ""numberProp"": 1,
    ""boolProp"": true
},
{
  ""stringProp"": ""world"",
  ""numberProp"": 2,
    ""boolProp"": false
}
]"
        );

    private static readonly ExampleInput.ExampleFileInput InputCSV
        = new(
            "input.csv",
            "CSV",
            "csv",
            "stringProp,numberProp,boolProp\r\nhello,1,true\r\nworld,2,false"
        );

    private static readonly ExampleInput.ExampleVariableInput CSVDelimiter =
        new ExampleInput.ExampleEnumVariableInput(
            "delimiter",
            "CSV",
            new ExampleInput.EnumValue("Comma", ","),
            new List<ExampleInput.EnumValue> { new("Comma", ","), new("Tab", "\t"), }
        );

    public static readonly ExampleInput.ExampleFileInput SchemaJson =
        new(
            "schema.json",
            "Schema",
            "json",
            JsonSerializer.Serialize(
                new JsonSchemaBuilder()
                    .Title("MySchema")
                    .AdditionalProperties(JsonSchema.False)
                    .Required("stringProp", "numberProp", "boolProp")
                    .Properties(
                        ("stringProp", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                        ("numberProp", new JsonSchemaBuilder().Type(SchemaValueType.String)),
                        ("boolProp", new JsonSchemaBuilder().Type(SchemaValueType.String))
                    )
                    .Build(),
                new JsonSerializerOptions() { WriteIndented = true }
            )
        );

    /// <summary>
    /// Input entities and set the to the entities variable
    /// </summary>
    public static ExampleComponent InputEntities = new ExampleComponent.Choice(
        new ExampleInput.Mode(
            "Input",
            "input",
            new List<ExampleComponent>()
            {
                new ExampleComponent.Sequence(
                    "CSV",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.Variable(
                            ExampleDefaults.CSVDelimiter with
                            {
                                Name = "inputCSVDelimiter", Group = "input"
                            }
                        ),
                        new ExampleComponent.File(
                            ExampleDefaults.InputCSV with { Group = "input" }
                        ),
                        new ExampleComponent.Constant(
                            "Read CSV",
                            new SetVariable<Array<Entity>>()
                            {
                                Variable = new VariableName("entities"),
                                Value = new FromCSV()
                                {
                                    Delimiter =
                                        new GetVariable<StringStream>()
                                        {
                                            Variable = new VariableName("inputCSVDelimiter")
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
                    "Concordance",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.File(
                            new(
                                "input.dat",
                                "input",
                                "concordance",
                                @"þstringPropþþnumberPropþþboolPropþ
þhelloþþ1þþtrueþ
þworldþþ2þþfalseþ"
                            )
                        ),
                        new ExampleComponent.Constant(
                            "Read CSV",
                            new SetVariable<Array<Entity>>()
                            {
                                Variable = new VariableName("entities"),
                                Value = new FromConcordance()
                                {
                                    Stream = new GetVariable<StringStream>()
                                    {
                                        Variable =
                                            new VariableName("inputdat")
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
                        new ExampleComponent.File(
                            ExampleDefaults.InputJson with { Group = "input" }
                        ),
                        new ExampleComponent.Constant(
                            "Read Json",
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
    );

    /// <summary>
    /// Convert entities in the entities variable to entities in the data variable
    /// </summary>
    public static ExampleComponent OutputEntities = new ExampleComponent.Choice(
        new ExampleInput.Mode(
            "Output",
            "output",
            new List<ExampleComponent>()
            {
                new ExampleComponent.Sequence(
                    "CSV",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.Variable(
                            ExampleDefaults.CSVDelimiter with
                            {
                                Name = "outputCSVDelimiter", Group = "output"
                            }
                        ),
                        new ExampleComponent.Constant(
                            "ToCsv",
                            new SetVariable<StringStream>()
                            {
                                Variable = new VariableName("data"),
                                Value = new ToCSV()
                                {
                                    Delimiter =
                                        new GetVariable<StringStream>()
                                        {
                                            Variable = new VariableName("outputCSVDelimiter")
                                        },
                                    Entities = new GetVariable<Array<Entity>>()
                                    {
                                        Variable =
                                            new VariableName("entities")
                                    }
                                }
                            }
                        )
                    }
                ),
                new ExampleComponent.Sequence(
                    "Concordance",
                    new List<ExampleComponent>()
                    {
                        new ExampleComponent.Constant(
                            "ToConcordance",
                            new SetVariable<StringStream>()
                            {
                                Variable = new VariableName("data"),
                                Value = new ToConcordance()
                                {
                                    Entities = new GetVariable<Array<Entity>>()
                                    {
                                        Variable =
                                            new VariableName("entities")
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
                        new ExampleComponent.Constant(
                            "ToJson",
                            new SetVariable<StringStream>()
                            {
                                Variable = new VariableName("data"),
                                Value = new ToJsonArray()
                                {
                                    Entities =
                                        new GetVariable<Array<Entity>>()
                                        {
                                            Variable =
                                                new VariableName("entities")
                                        }
                                }
                            }
                        )
                    }
                )
            }
        )
    );
}
