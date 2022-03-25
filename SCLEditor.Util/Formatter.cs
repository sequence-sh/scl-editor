using Reductech.Sequence.Core.LanguageServer;
using Reductech.Sequence.Core.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.Util;

public static class Formatter
{
    /// <summary>
    /// Format an SCL document
    /// </summary>
    public static List<IdentifiedSingleEditOperation> FormatDocument(
        string oldText,
        StepFactoryStore stepFactoryStore)
    {
        return
            FormattingHelper.FormatSCL(oldText, stepFactoryStore)
                .Select(Convert)
                .ToList();

        static IdentifiedSingleEditOperation Convert(SCLTextEdit textEdit)
        {
            var text = textEdit.NewText.Trim();

            var realRange = new Range(
                textEdit.StartLine + 1,
                textEdit.StartColumn + 1,
                textEdit.EndLine + 1,
                textEdit.EndColumn + 2 //Need to end one character later
            );

            return new IdentifiedSingleEditOperation() { Text = text, Range = realRange };
        }
    }
}
