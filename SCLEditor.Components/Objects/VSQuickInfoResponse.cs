﻿using Sequence.Core.LanguageServer.Objects;

namespace Sequence.Utilities.SCLEditor.Components.Objects;

public record VSQuickInfoResponse(IReadOnlyList<VSString> Contents)
{
    public VSQuickInfoResponse(QuickInfoResponse sclResponse) : this(
        sclResponse.MarkdownStrings.Select(x => new VSString(x)).ToList()
    ) { }
}
