using System.IO.Abstractions.TestingHelpers;

namespace Sequence.SCLEditor.Components;

/// <summary>
/// File metadata for the in-browser file system
/// </summary>
public record FileData(string Path, MockFileData Data)
{
    /// <summary>
    /// Creates a summary from the file's text content.
    /// </summary>
    public static string Truncate(string value, int maxLength, int maxLines = 2)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var split = value.Replace("\r", string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Take(maxLines)
            .Select(
                (s, i) => s.Length <= maxLength
                    ? (i + 1 == maxLines ? s + "..." : s)
                    : s[..maxLength] + "..."
            );

        return string.Join('\n', split);
    }

    /// <summary>
    /// Create human-readable file size from byte count
    /// </summary>
    public static string CalculateSize(int bc)
    {
        if (bc > 1024 * 1024 * 1024)
            return ((bc * 1.0) / 1024 * 1024 * 1024).ToString("F3") + "gb";

        if (bc > 1024 * 1024)
            return ((bc * 1.0) / 1024 * 1024).ToString("F3") + "mb";

        if (bc > 1024)
            return ((bc * 1.0) / 1024).ToString("F3") + "kb";

        return bc + "b";
    }

    /// <summary>
    /// Summary text
    /// </summary>
    public string TruncatedText => Truncate(Data.TextContents, 56);

    /// <summary>
    /// Byte size of the file's content
    /// </summary>
    public int ByteCount => Data.Contents.Length;

    /// <summary>
    /// Human-readable file size of the file's content
    /// </summary>
    public string SizeString => CalculateSize(ByteCount);
}
