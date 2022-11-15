using System.IO.Abstractions.TestingHelpers;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CSharpFunctionalExtensions;
using CSharpFunctionalExtensions.ValueTasks;
using Microsoft.Extensions.Logging.Abstractions;
using Sequence.Connectors.FileSystem;
using Sequence.Connectors.FileSystem.Steps;
using Sequence.Connectors.StructuredData;
using Sequence.Core;
using Sequence.Core.Abstractions;
using Sequence.Core.ExternalProcesses;
using Sequence.Core.Internal;
using Sequence.Core.LanguageServer;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// A page containing a dynamic scl example
/// </summary>
public sealed partial class DynamicExample : IDisposable
{
    /// <summary>
    /// The name of the Example (from the uri)
    /// </summary>
    [Parameter]
    public string ExampleName { get; set; } = null!;

    /// <summary>
    /// Whether this component should be rendered with dark mode
    /// </summary>
    [Parameter]
    public bool IsDarkMode
    {
        get => Themes.IsDarkMode.Value;
        set => Themes.IsDarkMode.OnNext(value);
    }

    //private MudTheme CurrentTheme { get; set; } = Themes.DefaultTheme;

    private MonacoEditor? _outputEditor = null!;

    private MonacoEditor _sclEditor = null!;

    private ExampleChoiceData _choiceData = new();

    private StepFactoryStore _stepFactoryStore = null!;

    private readonly ICompression _compression = new CompressionAdapter();

    private CancellationTokenSource? CancellationTokenSource { get; set; }

    private ExampleTemplate ExampleTemplate { get; set; } = null!;

    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        Themes.IsDarkMode
            .TakeUntil(_disposed)
            .Select(x => Observable.FromAsync(() => SetTheme(x)))
            .Switch()
            .Subscribe();
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        await base.OnParametersSetAsync();
        _outputEditor?.Dispose();

        var stepFactoryStoreResult = StepFactoryStore.TryCreateFromAssemblies(
            ExternalContext.Default,
            typeof(FileRead).Assembly,
            typeof(ToCSV).Assembly
        );

        _stepFactoryStore = stepFactoryStoreResult.Value;

        ExampleTemplate = ExampleList.AllExamples.Single(x => x.Url.Equals(ExampleName));

        _choiceData = ExampleChoiceData.Create(ExampleTemplate.ExampleComponent);
    }

    private async Task SetTheme(bool isDarkMode)
    {
        //CurrentTheme = isDarkMode ? Themes.DarkTheme : Themes.DefaultTheme;
        var theme = isDarkMode ? "vs-dark" : "vs";
        await MonacoEditorBase.SetTheme(theme);
        StateHasChanged();
    }

    private StandaloneEditorConstructionOptions GetOptions(ExampleInput.ExampleFileInput input)
    {
        return new()
        {
            AutomaticLayout         = true,
            Language                = input.Language,
            RenderControlCharacters = true,
            Value                   = input.InitialValue,
            TabSize                 = 8,
            UseTabStops             = true,
        };
    }

    private StandaloneEditorConstructionOptions OutputEditorConstructionOptions => new()
    {
        ReadOnly        = true,
        AutomaticLayout = true,
        WordWrap        = ExampleTemplate.ExampleOutput.WordWrap,
        Value           = "",
        Language        = ExampleTemplate.ExampleOutput.Language,
        TabSize         = 8,
        UseTabStops     = true,
    };

    private StandaloneEditorConstructionOptions SCLEditorConstructionOptions => new()
    {
        ReadOnly        = true,
        AutomaticLayout = true,
        WordWrap        = "off",
        Value           = "",
        Language        = "scl"
    };

    //public async Task FormatAll()
    //{
    //  foreach (var editor in _choiceData.Editors.Values.OfType<MonacoEditor>())
    //  {
    //    await editor.Trigger("anyString", "editor.action.formatDocument");
    //  }
    //}

    private async Task UpdateOutput()
    {
        if (_outputEditor is null)
            return;

        CancellationTokenSource?.Cancel();
        CancellationTokenSource = new CancellationTokenSource();

        var fileSystem = new MockFileSystem();

        foreach (var fileInput in ExampleTemplate.ExampleComponent.GetInputs(_choiceData)
                     .OfType<ExampleInput.ExampleFileInput>())
        {
            var editor = (MonacoEditor?)_choiceData.Editors[fileInput.Name];

            if (editor is not null)
            {
                var text = await editor.GetValue();
                fileSystem.AddFile(fileInput.Name, text);
            }
            else
            {
                fileSystem.AddFile(fileInput.Name, fileInput.InitialValue);
            }
        }

        var externalContext = new ExternalContext(
            ExternalProcessRunner.Instance,
            DefaultRestClientFactory.Instance,
            ConsoleAdapter.Instance,
            (ConnectorInjection.FileSystemKey, fileSystem),
            (ConnectorInjection.CompressionKey, _compression)
        );

        var state = new StateMonad(
            NullLogger.Instance,
            _stepFactoryStore,
            externalContext,
            new Dictionary<string, object>()
        );

        var sequence = ExampleTemplate.GetSequence(_choiceData);

        var sequenceSCL = FormattingHelper.Format(sequence).Trim();

        await _sclEditor.SetValue(sequenceSCL);

        var result = await sequence.Run<StringStream>(state, CancellationTokenSource.Token)
            .Map(x => x.GetStringAsync());

        var scl = sequence.Serialize(SerializeOptions.Serialize);
        Console.WriteLine(scl);

        if (result.IsSuccess)
        {
            await _outputEditor.SetValue(result.Value);
        }
        else
        {
            await _outputEditor.SetValue(result.Error.AsString);
        }
    }

    private readonly Subject<bool> _disposed = new();

    public void Dispose()
    {
        _disposed.OnNext(true);
    }
}
