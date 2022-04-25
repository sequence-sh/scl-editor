namespace Reductech.Utilities.SCLEditor.Components.Objects;

public enum CompletionTriggerKind
{
    Invoked = 1,
    TriggerCharacter = 2,

    [EditorBrowsable(EditorBrowsableState.Never)]
    TriggerForIncompleteCompletions = 3,
}
