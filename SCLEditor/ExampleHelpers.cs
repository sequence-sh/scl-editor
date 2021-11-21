using System.Collections.Generic;

namespace Reductech.Utilities.SCLEditor;

public static class ExampleHelpers
{
    public static IEnumerable<ExampleData> AllExamples
    {
        get
        {
            yield return ExampleData.Create(
                "Convert CSV to Json",
                "convertcsvtojson",
                "ToJsonArray (FromCsv (FileRead 'input.csv') delimiter: <delimiter>)",
                new ExampleOutput("Json", "off"),
                DefaultInputs.InputCSV,
                DefaultInputs.CSVDelimiter
            );

            yield return ExampleData.Create(
                "Convert Json to CSV",
                "convertjsontocsv",
                "ToCSV (FromJsonArray (FileRead 'input.json')) delimiter: <delimiter>",
                new ExampleOutput("csv", "off"),
                DefaultInputs.InputJson,
                DefaultInputs.CSVDelimiter
            );

            yield return ExampleData.Create(
                "Generate Schema from CSV",
                "generateschemafromcsv",
                "SchemaCreateCoerced SchemaName: 'Schema' AllowExtraProperties: false Entities: (FromCSV (FileRead 'input.csv') delimiter: <delimiter>)",
                new ExampleOutput("json", "off"),
                DefaultInputs.InputCSV,
                DefaultInputs.CSVDelimiter
            );

            yield return ExampleData.Create(
                "Generate Schema from json",
                "generateschemafromjson",
                "SchemaCreateCoerced SchemaName: 'Schema' AllowExtraProperties: false Entities: (fromjsonarray (FileRead 'input.json'))",
                new ExampleOutput("json", "off"),
                DefaultInputs.InputJson
            );

            yield return ExampleData.Create(
                "Schema Validate CSV",
                "schemavalidatecsv",
                "- Foreach (Validate EntityStream:(FromCSV (FileRead 'input.csv') delimiter: <delimiter>) Schema: (FromJson (FileRead 'schema.json')) ) (<> => DoNothing)\r\n- 'Validation Successful'",
                new ExampleOutput("json", "off"),
                DefaultInputs.InputCSV,
                DefaultInputs.CSVDelimiter,
                DefaultInputs.SchemaJson
            );

            yield return ExampleData.Create(
                "Schema Validate JSON",
                "schemavalidatejson",
                "- Foreach (Validate EntityStream:(FromJsonArray (FileRead 'input.json')) Schema: (FromJson (FileRead 'schema.json')) ) (<> => DoNothing)\r\n- 'Validation Successful'",
                new ExampleOutput("json", "off"),
                DefaultInputs.InputJson,
                DefaultInputs.SchemaJson
            );
        }
    }
}
