using System;
using System.Collections.Generic;
using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.Utilities.SCLEditor.LanguageServer.Objects;

namespace Reductech.Utilities.SCLEditor.LanguageServer;

/// <summary>
/// Visits SCL for completion
/// </summary>
public class CompletionVisitor : SCLBaseVisitor<CompletionResponse?>
{
    /// <summary>
    /// Creates a new Completion Visitor
    /// </summary>
    public CompletionVisitor(LinePosition position, StepFactoryStore stepFactoryStore)
    {
        Position         = position;
        StepFactoryStore = stepFactoryStore;
    }

    /// <summary>
    /// The position
    /// </summary>
    public LinePosition Position { get; }

    /// <summary>
    /// The Step Factory Store
    /// </summary>
    public StepFactoryStore StepFactoryStore { get; }

    /// <inheritdoc />
    public override CompletionResponse? VisitChildren(IRuleNode node)
    {
        var i = 0;

        while (i < node.ChildCount)
        {
            var child = node.GetChild(i);

            if (child is TerminalNodeImpl tni && tni.GetText() == "<EOF>")
            {
                break;
            }

            if (child is ParserRuleContext prc)
            {
                if (prc.StartsAfter(Position))
                {
                    break;
                }
                else if (prc.ContainsPosition(Position))
                {
                    var result = Visit(child);

                    if (result is not null)
                        return result;
                }
            }

            i++;
        }

        if (i >= 1) //Go back to the last function and use that
        {
            var lastChild = node.GetChild(i - 1);

            var r = Visit(lastChild);
            return r;
        }

        return null;
    }

    /// <inheritdoc />
    public override CompletionResponse? VisitErrorNode(IErrorNode node)
    {
        if (node.Symbol.ContainsPosition(Position))
        {
            return base.VisitErrorNode(node);
        }

        return base.VisitErrorNode(node);
    }

    /// <inheritdoc />
    public override CompletionResponse? VisitFunction1(SCLParser.Function1Context context)
    {
        var func = context.function();

        var result = VisitFunction(func);

        return result;
    }

    /// <inheritdoc />
    public override CompletionResponse? VisitFunction(SCLParser.FunctionContext context)
    {
        var name = context.NAME().GetText();

        if (!context.ContainsPosition(Position))
        {
            if (context.EndsBefore(Position))
            {
                //Assume this is another parameter to this function
                if (StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                    return StepParametersCompletionResponse(
                        stepFactory,
                        new TextRange(Position, Position)
                    );
            }

            return null;
        }

        if (context.NAME().Symbol.ContainsPosition(Position))
        {
            var nameText = context.NAME().GetText();

            var options =
                StepFactoryStore.Dictionary
                    .Where(x => x.Key.Contains(nameText, StringComparison.OrdinalIgnoreCase))
                    .GroupBy(x => x.Value, x => x.Key)
                    .ToList();

            return ReplaceWithSteps(options, context.NAME().Symbol.GetRange());
        }

        var positionalTerms = context.term();

        for (var index = 0; index < positionalTerms.Length; index++)
        {
            var term = positionalTerms[index];

            if (term.ContainsPosition(Position))
            {
                return Visit(term);
            }
        }

        foreach (var namedArgumentContext in context.namedArgument())
        {
            if (namedArgumentContext.ContainsPosition(Position))
            {
                if (namedArgumentContext.NAME().Symbol.ContainsPosition(Position))
                {
                    if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                        return null; //Don't know what step factory to use

                    var range = namedArgumentContext.NAME().Symbol.GetRange();

                    return StepParametersCompletionResponse(stepFactory, range);
                }

                return Visit(namedArgumentContext);
            }
        }

        {
            if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                return null; //No clue what name to use

            return StepParametersCompletionResponse(stepFactory, new TextRange(Position, Position));
        }
    }

    private static CompletionResponse ReplaceWithSteps(
        IEnumerable<IGrouping<IStepFactory, string>> stepFactories,
        TextRange range)
    {
        var items = stepFactories.SelectMany(CreateCompletionItems).ToList();

        return new CompletionResponse() { Items = items, IsIncomplete = false };

        IEnumerable<CompletionItem> CreateCompletionItems(IGrouping<IStepFactory, string> factory)
        {
            var documentation = Helpers.GetMarkDownDocumentation(factory);

            foreach (var key in factory)
            {
                yield return new()
                {
                    TextEdit = new LinePositionSpanTextChange()
                    {
                        NewText     = key,
                        StartLine   = range.StartLineNumber,
                        EndLine     = range.EndLineNumber,
                        StartColumn = range.StartColumn,
                        EndColumn   = range.EndColumn,
                    },
                    Label            = key,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    Detail           = factory.Key.Summary,
                    Documentation    = documentation
                };
            }
        }
    }

    /// <summary>
    /// Gets the step parameter completion list
    /// </summary>
    public static CompletionResponse StepParametersCompletionResponse(
        IStepFactory stepFactory,
        TextRange range)
    {
        var documentation = Helpers.GetMarkDownDocumentation(stepFactory);

        var options =
            stepFactory.ParameterDictionary
                .Where(x => x.Key is StepParameterReference.Named)
                .Select(x => CreateCompletionItem(x.Key, x.Value))
                .ToList();

        CompletionItem CreateCompletionItem(
            StepParameterReference stepParameterReference,
            IStepParameter stepParameter)
        {
            return new()
            {
                TextEdit = new LinePositionSpanTextChange()
                {
                    NewText     = stepParameterReference.Name + ":",
                    StartLine   = range.StartLineNumber,
                    StartColumn = range.StartColumn,
                    EndLine     = range.EndLineNumber,
                    EndColumn   = range.EndColumn
                },
                Label            = stepParameterReference.Name,
                InsertTextFormat = InsertTextFormat.PlainText,
                Detail           = stepParameter.Summary,
                Documentation    = documentation
            };
        }

        return new CompletionResponse() { Items = options, IsIncomplete = false };
    }
}
