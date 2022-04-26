using Reductech.Sequence.Core.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.Components.Objects;

/// <summary>
/// A position in monaco. The LineNumber and Column are both 1-indexed
/// </summary>
public record VSPosition(int LineNumber, int Column)
{
    /// <summary>
    /// Get the SCL Line position
    /// </summary>
    /// <returns></returns>
    public LinePosition AsLinePosition() => new(LineNumber - 1, Column - 1);
}
