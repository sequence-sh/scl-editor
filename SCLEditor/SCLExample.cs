using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Reductech.Utilities.SCLEditor;

public record SCLExample(string Name, string Text)
{
    public const string InputFilePath = "input.txt";

    public static IEnumerable<SCLExample> All
    {
        get
        {
            var resourceSet = ExamplesResource.ResourceManager.GetResourceSet(
                CultureInfo.InvariantCulture,
                true,
                false
            );

            foreach (DictionaryEntry entry in resourceSet)
            {
                string text;

                if (entry.Value is byte[] byteArray)
                {
                    text = Encoding.UTF8.GetString(byteArray);
                }
                else
                {
                    text = entry.Value?.ToString();
                }

                var example = new SCLExample(entry.Key?.ToString(), text);
                yield return example;
            }
        }
    }
}
