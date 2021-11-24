using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Reductech.Utilities.SCLEditor.LanguageServer.Objects;

internal class ZeroBasedIndexConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(int)
                                                     || objectType == typeof(int?)
                                                     || objectType == typeof(IEnumerable<int>)
                                                     || objectType == typeof(int[]);

    public override object? ReadJson(
        JsonReader reader,
        Type objectType,
        object? existingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        if (objectType == typeof(int[]))
        {
            var numArray = serializer.Deserialize<int[]>(reader) ?? Array.Empty<int>();

            for (var index = 0; index < numArray.Length; ++index)
                numArray[index] = numArray[index] - 1;

            return numArray;
        }

        if (objectType == typeof(IEnumerable<int>))
            return serializer.Deserialize<IEnumerable<int>>(reader) ?? Array.Empty<int>()
                .Select(x => x - 1);

        var nullable = serializer.Deserialize<int?>(reader);

        return nullable - 1;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
            serializer.Serialize(writer, null);
        else
        {
            var type = value.GetType();

            if (type == typeof(int[]))
            {
                var numArray = (int[])value;

                for (var index = 0; index < numArray.Length; ++index)
                    numArray[index] = numArray[index] + 1;
            }
            else if (type == typeof(IEnumerable<int>))
                value = ((IEnumerable<int>)value).Select(x => x + 1);
            else if (type == typeof(int?))
            {
                var nullable = (int?)value;
                value = nullable + 1;
            }
            else if (type == typeof(int))
                value = (int)value + 1;

            serializer.Serialize(writer, value);
        }
    }
}
