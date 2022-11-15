using Sequence.Core.LanguageServer.Objects;

namespace Sequence.Utilities.SCLEditor.Components.Objects;

public record VSSignatureParameter(string Label, string Name, VSString Documentation)
{
    public VSSignatureParameter(SignatureHelpParameter parameter) : this(
        parameter.Label,
        parameter.Name,
        new VSString(parameter.Documentation)
    ) { }
}
