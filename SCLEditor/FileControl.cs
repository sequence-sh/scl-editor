using System.IO.Abstractions.TestingHelpers;

namespace Reductech.Utilities.SCLEditor;

public record FileData(
    string Path,
    MockFileData Data)
{
    public static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.Length <= maxLength)
            return value;
        else
            return value.Substring(0, maxLength) + "...";
    }

    public string TruncatedText => Truncate(Data.TextContents, 100);

    public int ByteCount => Data.Contents.Length;

    public string SizeString
    {
        get
        {
            var bc = ByteCount;

            if (bc > 1024 * 1024 * 1024)
                return ((bc * 1.0) / 1024 * 1024 * 1024).ToString("F3") + "gb";

            if (bc > 1024 * 1024)
                return ((bc * 1.0) / 1024 * 1024).ToString("F3") + "mb";

            if (bc > 1024)
                return ((bc * 1.0) / 1024).ToString("F3") + "kb";

            return bc + "b";
        }
    }
}
