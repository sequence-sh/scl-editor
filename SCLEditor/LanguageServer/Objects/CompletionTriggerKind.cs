using System.ComponentModel;

namespace Reductech.Utilities.SCLEditor.LanguageServer.Objects;

public enum CompletionTriggerKind
{
    Invoked = 1,
    TriggerCharacter = 2,

    [EditorBrowsable(EditorBrowsableState.Never)]
    TriggerForIncompleteCompletions = 3,
}
