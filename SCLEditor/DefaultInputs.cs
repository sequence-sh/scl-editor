using System.Collections.Generic;
using System.Text.Json;
using Json.Schema;

namespace Reductech.Utilities.SCLEditor;

public static class DefaultInputs
{
    public static readonly ExampleInput.ExampleFileInput InputJson
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

    public static readonly ExampleInput.ExampleFileInput InputCSV
        = new(
            "input.csv",
            "CSV",
            "csv",
            "stringProp,numberProp,boolProp\r\nhello,1,true\r\nworld,2,false"
        );

    public static readonly ExampleInput.ExampleVariableInput CSVDelimiter =
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
                        ("numberProp", new JsonSchemaBuilder().Type(SchemaValueType.Integer)),
                        ("boolProp", new JsonSchemaBuilder().Type(SchemaValueType.Boolean))
                    )
                    .Build(),
                new JsonSerializerOptions() { WriteIndented = true }
            )
        );
}
