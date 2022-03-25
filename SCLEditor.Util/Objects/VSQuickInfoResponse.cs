using Reductech.Sequence.Core.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.Util.Objects;

public record VSQuickInfoResponse(IReadOnlyList<VSString> Contents)
{
    public VSQuickInfoResponse(QuickInfoResponse sclResponse) : this(
        sclResponse.MarkdownStrings.Select(x => new VSString(x)).ToList()
    ) { }
}
