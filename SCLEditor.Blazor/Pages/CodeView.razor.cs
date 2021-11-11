using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlazorMonaco;
using CSharpFunctionalExtensions;
using JetBrains.Annotations;
using MELT;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Reductech.EDR.Connectors.FileSystem;
using Reductech.EDR.Connectors.StructuredData;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.ExternalProcesses;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Parser;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;
using TypeReference = Reductech.EDR.Core.Internal.TypeReference;

namespace Reductech.Utilities.SCLEditor.Blazor.Pages
{

public partial class CodeView
{
    [Inject] public IJSRuntime Runtime { get; set; } = null!;

    private MonacoEditor _editor;

    private ITestLoggerFactory _testLoggerFactory =
        TestLoggerFactory.Create(x => x.FilterByMinimumLevel(LogLevel.Information));

    private readonly MockFileSystem _fileSystem = new();

    private readonly ICompression _compression = new CompressionAdapter();

    private StringBuilder _consoleStringBuilder = new();

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Console.SetOut(new StringWriter(_consoleStringBuilder));
        base.OnInitialized();
    }

    public void ClearLogs()
    {
        _testLoggerFactory.Sink.Clear();
    }

    public string LogText()
    {
        var text =
            string.Join(
                "\r\n",
                _testLoggerFactory.Sink.LogEntries.Select(x => x.Message)
            );

        return text;
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

    public CancellationTokenSource? CancellationTokenSource { get; set; }

    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = null;
    }

    public IExternalContext GetExternalContext()
    {
        //var fileSystem = new FileSelectionFileSystem(_fileDictionary);

        return new ExternalContext(
            ExternalProcessRunner.Instance,
            DefaultRestClientFactory.Instance,
            ConsoleAdapter.Instance,
            (ConnectorInjection.FileSystemKey, _fileSystem),
            (ConnectorInjection.CompressionKey, _compression)
        ); //TODO add file system connector
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

        var logger          = _testLoggerFactory.CreateLogger("SCL");
        var externalContext = GetExternalContext();

        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            externalContext,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        if (stepFactoryStoreResult.IsFailure)
        {
            _consoleStringBuilder.AppendLine(stepFactoryStoreResult.Error.AsString);
            CancellationTokenSource = null;
            _consoleStringBuilder.AppendLine();
            return;
        }

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, stepFactoryStoreResult.Value));

        if (stepResult.IsFailure)
        {
            _consoleStringBuilder.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            await using var stateMonad = new StateMonad(
                logger,
                stepFactoryStoreResult.Value,
                externalContext,
                new Dictionary<string, object>()
            );

            var runResult = await stepResult.Value.Run<object>(
                stateMonad,
                CancellationTokenSource.Token
            );

            if (runResult.IsFailure)
                _consoleStringBuilder.AppendLine(runResult.Error.AsString);

            else if (runResult.Value is Unit)
                _consoleStringBuilder.AppendLine("Sequence Completed Successfully");
            else
            {
                _consoleStringBuilder.AppendLine(
                    $"Sequence Completed Successfully with result: '{runResult.Value}'"
                );
            }
        }

        CancellationTokenSource = null;

        _consoleStringBuilder.AppendLine();
    }

    private StandaloneEditorConstructionOptions EditorConstructionOptions(MonacoEditor editor)
    {
        return new() { AutomaticLayout = true, Language = "scl", Value = "print 123" };
    }
}

}
