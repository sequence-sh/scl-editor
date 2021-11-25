namespace Reductech.Utilities.SCLEditor.LanguageServer.Objects;

public class CompletionResponse
{
    public bool IsIncomplete { get; set; }

    public IReadOnlyList<CompletionItem> Items { get; set; }
}
