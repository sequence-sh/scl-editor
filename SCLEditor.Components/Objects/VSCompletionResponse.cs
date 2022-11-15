using Sequence.Core.LanguageServer.Objects;

namespace Sequence.Utilities.SCLEditor.Components.Objects;

public record VSCompletionResponse(List<VSCompletionItem> Suggestions, bool IsIncomplete)
{
    public VSCompletionResponse(CompletionResponse r) : this(
        r.Items.Select(x => new VSCompletionItem(x)).ToList(),
        r.IsIncomplete
    ) { }
}
