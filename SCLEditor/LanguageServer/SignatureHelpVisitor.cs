﻿using System.Linq;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;

namespace Reductech.Utilities.SCLEditor.LanguageServer;

/// <summary>
/// Visits SCL to get Signature Help
/// </summary>
public class SignatureHelpVisitor : SCLBaseVisitor<SignatureHelp?>
{
    /// <inheritdoc />
    public SignatureHelpVisitor(Position position, StepFactoryStore stepFactoryStore)
    {
        Position         = position;
        StepFactoryStore = stepFactoryStore;
    }

    /// <summary>
    /// The position to get Signature Help at
    /// </summary>
    public Position Position { get; }

    /// <summary>
    /// The Step Factory Store
    /// </summary>
    public StepFactoryStore StepFactoryStore { get; }

    /// <inheritdoc />
    public override SignatureHelp? Visit(IParseTree tree)
    {
        if (tree is ParserRuleContext context)
        {
            if (context.ContainsPosition(Position))
            {
                return base.Visit(tree);
            }
            else if (context.EndsBefore(Position) && !context.HasSiblingsAfter(Position))
            {
                //This position is at the end of this line - enter anyway
                var result = base.Visit(tree);
                return result;
            }
        }

        return DefaultResult;
    }

    /// <inheritdoc />
    protected override bool ShouldVisitNextChild(IRuleNode node, SignatureHelp? currentResult)
    {
        if (currentResult is not null)
            return false;

        return true;
    }

    /// <inheritdoc />
    public override SignatureHelp? VisitFunction(SCLParser.FunctionContext context)
    {
        var name = context.NAME().GetText();

        if (!context.ContainsPosition(Position))
        {
            if (context.EndsBefore(Position) &&
                context.Stop.IsSameLineAs(
                    Position
                )) //This position is on the line after the step definition
            {
                if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                    return null; //No clue what name to use

                var result = StepParametersSignatureHelp(stepFactory);
                return result;
            }

            return null;
        }

        if (context.NAME().Symbol.ContainsPosition(Position))
        {
            return null;
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

                    //var range = namedArgumentContext.NAME().Symbol.GetRange();

                    return StepParametersSignatureHelp(stepFactory);
                }

                return Visit(namedArgumentContext);
            }
        }

        {
            if (!StepFactoryStore.Dictionary.TryGetValue(name, out var stepFactory))
                return null; //No clue what name to use

            return StepParametersSignatureHelp(stepFactory);
        }
    }

    private static SignatureHelp StepParametersSignatureHelp(IStepFactory stepFactory)
    {
        var documentation = Helpers.GetMarkDownDocumentation(stepFactory);

        var options =
            stepFactory.ParameterDictionary.Keys
                .OfType<StepParameterReference.Named>()
                .Select(CreateCompletionItem)
                .ToList();

        ParameterInformation CreateCompletionItem(
            StepParameterReference.Named stepParameterReference)
        {
            return new()
            {
                Label = stepParameterReference.Name,
                Documentation = new StringOrMarkupContent(
                    new MarkupContent() { Kind = MarkupKind.Markdown, Value = documentation }
                )
            };
        }

        var signatureHelp = new SignatureHelp()
        {
            Signatures = new Container<SignatureInformation>(
                new SignatureInformation
                {
                    Label         = stepFactory.TypeName,
                    Documentation = stepFactory.Summary,
                    Parameters    = new Container<ParameterInformation>(options)
                }
            )
        };

        return signatureHelp;
    }
}
