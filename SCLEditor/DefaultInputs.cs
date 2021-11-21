using System.Collections.Generic;
using System.Text.Json;
using Json.Schema;

namespace Reductech.Utilities.SCLEditor;

public static class DefaultInputs
{
    public static readonly ExampleInput.ExampleFileInput InputJson
        = new(
            "input.json",
            "json",
            "[{\"a\": true, \"b\": 1}]",
            null
        );

    public static readonly ExampleInput.ExampleFileInput InputCSV
        = new(
            "input.csv",
            "csv",
            "string,number,bool\r\nhello,1,true\r\nworld,2,false",
            null
        );

    public static readonly ExampleInput.ExampleVariableInput CSVDelimiter =
        new ExampleInput.ExampleEnumVariableInput(
            "delimiter",
            null,
            ",",
            new ExampleInput.BoundValue<string>(),
            new List<string>() { ",", "\t" }
        );

    public static readonly ExampleInput.ExampleFileInput SchemaJson =
        new(
            "schema.json",
            "json",
            JsonSerializer.Serialize(
                new JsonSchemaBuilder()
                    .Title("MySchema")
                    .AdditionalProperties(JsonSchema.False)
                    .Required("a", "b")
                    .Properties(
                        ("a", new JsonSchemaBuilder().Type(SchemaValueType.Boolean)),
                        ("b", new JsonSchemaBuilder().Type(SchemaValueType.Integer))
                    )
                    .Build()
            ),
            null
        );
}
