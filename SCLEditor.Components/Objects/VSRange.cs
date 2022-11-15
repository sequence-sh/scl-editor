using Sequence.Core.LanguageServer.Objects;

namespace Sequence.Utilities.SCLEditor.Components.Objects;

/// <summary>
/// A range of characters in the monarch editor
/// </summary>
public record VSRange(int StartLineNumber, int StartColumn, int EndLineNumber, int EndColumn)
{
    /// <summary>
    /// Create a MonarchRange from a text edit
    /// </summary>
    /// <param name="textEdit"></param>
    public VSRange(SCLTextEdit textEdit) : this(
        textEdit.StartLine + 1,
        textEdit.StartColumn + 1,
        textEdit.EndLine + 1,
        textEdit.EndColumn + 1
    ) { }
}
