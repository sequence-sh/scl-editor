namespace Reductech.Utilities.SCLEditor.Components.Objects;

public record VSCompletionRequest(
    VSPosition Position,
    CompletionTriggerKind CompletionTrigger,
    char? TriggerCharacter) : VSRequest(Position);
