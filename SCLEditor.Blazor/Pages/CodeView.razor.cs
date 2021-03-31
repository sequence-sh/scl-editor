using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlazorMonaco;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Util;
using TypeReference = Reductech.EDR.Core.Internal.TypeReference;

namespace Reductech.Utilities.SCLEditor.Blazor.Pages
{

public partial class CodeView
{
    //public string SCLText { get; set; } = "";

    [Inject] public IJSRuntime Runtime { get; set; }

    private MonacoEditor _editor;

    private readonly Dictionary<string, BrowserFile> _fileDictionary =
        new(StringComparer.OrdinalIgnoreCase);

    private StringBuilder _consoleStringBuilder = new();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Console.SetOut(new StringWriter(_consoleStringBuilder));
        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await MonacoEditorBase.SetTheme("vs-dark");

            await Runtime.InvokeVoidAsync(
                "registerSCL"
            ); // The function in index.html is called that

            var model = await _editor.GetModel();
            await MonacoEditorBase.SetModelLanguage(model, "scl");
        }

        await base.OnInitializedAsync();
    }

    [CanBeNull] public CancellationTokenSource? CancellationTokenSource { get; set; }

    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = null;
    }

    public IExternalContext GetExternalContext()
    {
        var fileSystem = new FileSelectionFileSystem(_fileDictionary);

        return new ExternalContext(
            fileSystem,
            ExternalContext.Default.ExternalProcessRunner,
            ExternalContext.Default.Console
        );
    }

    public async Task SetSCL(string s)
    {
        await _editor.SetValue(s);
    }

    public async Task Run()
    {
        var sclText = await _editor.GetValue();

        CancellationTokenSource?.Cancel();
        var cts = new CancellationTokenSource();
        CancellationTokenSource = cts;

        var result = await RunSequenceFromTextAsync(
            sclText,
            SCLSettings.EmptySettings,
            GetExternalContext(),
            NullLogger.Instance,
            StepFactoryStore.CreateUsingReflection(),
            cts.Token
        );

        CancellationTokenSource = null;

        if (result is Unit)
            _consoleStringBuilder.AppendLine("Sequence Completed Successfully");
        else
        {
            _consoleStringBuilder.AppendLine(
                $"Sequence Completed Successfully with result: '{result}'"
            );
        }

        _consoleStringBuilder.AppendLine();
    }

    public async Task<object> RunSequenceFromTextAsync(
        string text,
        SCLSettings settings,
        IExternalContext externalContext,
        ILogger logger,
        StepFactoryStore stepFactoryStore,
        CancellationToken cancellationToken)
    {
        var stepResult = SCLParsing.ParseSequence(text)
            .Bind(x => x.TryFreeze(TypeReference.Any.Instance, stepFactoryStore));

        if (stepResult.IsFailure)
            return stepResult.Error.AsString;

        await using var stateMonad = new StateMonad(
            logger,
            settings,
            stepFactoryStore,
            externalContext,
            new Dictionary<string, object>()
        );

        var runResult = await stepResult.Value.Run<object>(stateMonad, cancellationToken);

        if (runResult.IsFailure)
            return runResult.Error.AsString;

        return runResult.Value;
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true, Language = "scl", Value = "print 123"
        };
    }
}

}
