using Reductech.Sequence.Core.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.Util.Objects;

public record VSCompletionItem(
    string Label,
    int Kind,
    string Detail,
    VSString Documentation,
    List<string>? CommitCharacters,
    bool Preselect,
    string? FilterText,
    string InsertText,
    VSRange Range,
    List<string>? Tags,
    string? SortText,
    List<object>? AdditionalTextEdits,
    bool KeepWhitespace)
{
    /// <summary>
    /// Create a CompletionItem from an SCL Completion Item
    /// </summary>
    /// <param name="s"></param>
    public VSCompletionItem(CompletionItem s)
        : this(
            s.Label,
            0,
            s.Detail,
            new VSString(s.Documentation),
            null,
            s.Preselect,
            null,
            s.TextEdit.NewText,
            new VSRange(s.TextEdit),
            null,
            null,
            null,
            true
        ) { }
}
