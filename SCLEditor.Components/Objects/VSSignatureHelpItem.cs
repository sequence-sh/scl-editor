using Sequence.Core.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.Components.Objects;

public record VSSignatureHelpItem(
    string Label,
    VSString Documentation,
    List<VSSignatureParameter> Parameters)
{
    public VSSignatureHelpItem(SignatureHelpItem signatureHelpItem)
        : this(
            signatureHelpItem.Label,
            new VSString(signatureHelpItem.Documentation),
            signatureHelpItem.Parameters.Select(x => new VSSignatureParameter(x)).ToList()
        ) { }
}
