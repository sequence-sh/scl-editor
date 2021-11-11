using System.Collections.Generic;

namespace Reductech.Utilities.SCLEditor;

public record SCLExample(string Name, string Text)
{
    public const string InputFilePath = "input.txt";

    public static IEnumerable<SCLExample> All
    {
        get
        {
            yield return new SCLExample(
                "CSV to Json",
                $@"
ReadFile '{InputFilePath}'
| FromCSV
| ToJsonArray
"
            );

            yield return new SCLExample(
                "Json to CSV",
                $@"
ReadFile '{InputFilePath}'
| FromJSONArray
| ToCSV
"
            );
        }
    }
}
