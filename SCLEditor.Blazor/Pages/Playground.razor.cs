using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlazorMonaco;
using CSharpFunctionalExtensions;
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

namespace Reductech.Utilities.SCLEditor.Blazor.Pages
{

public partial class Playground
{
    [Inject] public IJSRuntime Runtime { get; set; } = null!;

    private FileSelection _fileSelection;

    private MonacoEditor _sclEditor;

    private MonacoEditor _fileEditor;

    bool OutputExpanded { get; set; } = true;
    bool LogExpanded { get; set; } = true;

    private readonly ITestLoggerFactory _testLoggerFactory =
        TestLoggerFactory.Create(x => x.FilterByMinimumLevel(LogLevel.Information));

    private readonly MockFileSystem _fileSystem = new();

    private readonly ICompression _compression = new CompressionAdapter();

    private readonly StringBuilder _consoleStringBuilder = new();

    private StepFactoryStore _stepFactoryStore;
    private IExternalContext _externalContext;

    public CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Console.SetOut(new StringWriter(_consoleStringBuilder));

        _externalContext = new ExternalContext(
            ExternalProcessRunner.Instance,
            DefaultRestClientFactory.Instance,
            ConsoleAdapter.Instance,
            (ConnectorInjection.FileSystemKey, _fileSystem),
            (ConnectorInjection.CompressionKey, _compression)
        );

        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            _externalContext,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        _stepFactoryStore = stepFactoryStoreResult.Value;

        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            //await MonacoEditorBase.SetTheme("vs-dark");

            await Runtime.InvokeVoidAsync(
                "registerSCL"
            ); // The function in index.html is called that

            var model = await _sclEditor.GetModel();
            await MonacoEditorBase.SetModelLanguage(model, "scl");
        }

        await base.OnAfterRenderAsync(firstRender);
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

    private void OnModelChanged(ModelChangedEvent obj) { }

    public void Cancel()
    {
        CancellationTokenSource?.Cancel();
        CancellationTokenSource = null;
    }

    public async Task SetSCL(string s)
    {
        await _sclEditor.SetValue(s);
    }

    public async Task Run()
    {
        var sclText = await _sclEditor.GetValue();

        CancellationTokenSource?.Cancel();
        var cts = new CancellationTokenSource();
        CancellationTokenSource = cts;

        var logger = _testLoggerFactory.CreateLogger("SCL");

        var stepResult = SCLParsing.TryParseStep(sclText)
            .Bind(x => x.TryFreeze(SCLRunner.RootCallerMetadata, _stepFactoryStore));

        if (stepResult.IsFailure)
        {
            _consoleStringBuilder.AppendLine(stepResult.Error.AsString);
        }
        else
        {
            await using var stateMonad = new StateMonad(
                logger,
                _stepFactoryStore,
                _externalContext,
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
                _consoleStringBuilder.AppendLine(runResult.Value.ToString());
            }
        }

        CancellationTokenSource = null;

        _consoleStringBuilder.AppendLine();
    }

    private StandaloneEditorConstructionOptions SCLEditorConstructionOptions(MonacoEditor _)
    {
        return new() { AutomaticLayout = true, Language = "scl", Value = "print 123" };
    }

    private StandaloneEditorConstructionOptions FileEditorConstructionOptions(MonacoEditor _)
    {
        if (_fileSelection?.SelectedFile is not null)
        {
            var extension = GetLanguageFromFileExtension(
                _fileSystem.Path.GetExtension(_fileSelection.SelectedFile.Path)
            );

            return new()
            {
                AutomaticLayout = true,
                Language        = extension,
                Value           = _fileSelection.SelectedFile.Data.TextContents
            };
        }

        return new() { AutomaticLayout = true };
    }

    private string GetLanguageFromFileExtension(string extension)
    {
        return extension?.ToLowerInvariant() switch

        {
            "yml"  => "yaml",
            "yaml" => "yaml",
            "json" => "json",
            "cs"   => "csharp",
            _      => ""
        };
    }
}

}
