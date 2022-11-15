namespace Sequence.Utilities.SCLEditor.Components.Objects;

/// <summary>
/// A visual studio code completion request
/// </summary>
public record VSCompletionRequest(
    VSPosition Position,
    CompletionTriggerKind CompletionTrigger,
    char? TriggerCharacter) : VSRequest(Position);
