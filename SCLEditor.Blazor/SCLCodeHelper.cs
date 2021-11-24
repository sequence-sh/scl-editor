namespace Reductech.Utilities.SCLEditor.Blazor;

/// <summary>
/// Configuration for the SCL editor
/// </summary>
public class EditorConfiguration : INotifyPropertyChanged
{
    private bool _completionEnabled = true;

    /// <summary>
    /// Whether code completion is enabled
    /// </summary>
    public bool CompletionEnabled
    {
        get => _completionEnabled;
        set
        {
            if (value == _completionEnabled)
                return;

            _completionEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _signatureHelpEnabled = true;

    /// <summary>
    /// Whether Signature help is enabled
    /// </summary>
    public bool SignatureHelpEnabled
    {
        get => _signatureHelpEnabled;
        set
        {
            if (value == _signatureHelpEnabled)
                return;

            _signatureHelpEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _quickInfoEnabled = true;

    /// <summary>
    /// Whether quick info is enabled
    /// </summary>
    public bool QuickInfoEnabled
    {
        get => _quickInfoEnabled;
        set
        {
            if (value == _quickInfoEnabled)
                return;

            _quickInfoEnabled = value;
            OnPropertyChanged();
        }
    }

    private bool _diagnosticsEnabled = true;

    /// <summary>
    /// Whether diagnostics is enabled
    /// </summary>
    public bool DiagnosticsEnabled
    {
        get => _diagnosticsEnabled;
        set
        {
            if (value == _diagnosticsEnabled)
                return;

            _diagnosticsEnabled = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// Contains JS Invokable methods for working with SCL
/// </summary>
public class SCLCodeHelper
{
    /// <summary>
    /// Create a new SCL Code Helper
    /// </summary>
    public SCLCodeHelper(StepFactoryStore stepFactoryStore, EditorConfiguration configuration)
    {
        StepFactoryStore = stepFactoryStore;
        Configuration    = configuration;
    }

    /// <summary>
    /// The Step factory store
    /// </summary>
    public StepFactoryStore StepFactoryStore { get; }

    /// <summary>
    /// The editor configuration
    /// </summary>
    public EditorConfiguration Configuration { get; }

    /// <summary>
    /// Gets code completion from a completion request
    /// </summary>
    [JSInvokable]
    public async Task<CompletionResponse> GetCompletionAsync(
        string code,
        CompletionRequest completionRequest)
    {
        if (!Configuration.CompletionEnabled)
            return new CompletionResponse();

        var position = new LinePosition(completionRequest.Line, completionRequest.Column);
        var visitor  = new CompletionVisitor(position, StepFactoryStore);

        var completionList = visitor.LexParseAndVisit(
            code,
            x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); }
        ) ?? new CompletionResponse();

        return completionList;
    }

    /// <summary>
    /// Resolve a completion request
    /// </summary>
    [JSInvokable]
    public async Task<CompletionResolveResponse> GetCompletionResolveAsync(
        CompletionResolveRequest completionResolveRequest)
    {
        return new CompletionResolveResponse { Item = completionResolveRequest.Item };
    }

    /// <summary>
    /// Gets signature help
    /// </summary>
    [JSInvokable]
    public async Task<SignatureHelpResponse> GetSignatureHelpAsync(
        string code,
        SignatureHelpRequest signatureHelpRequest)
    {
        if (!Configuration.SignatureHelpEnabled)
            return new SignatureHelpResponse();

        var visitor = new SignatureHelpVisitor(
            new LinePosition(signatureHelpRequest.Line, signatureHelpRequest.Column),
            StepFactoryStore
        );

        var signatureHelp = visitor.LexParseAndVisit(
            code,
            x => { x.RemoveErrorListeners(); },
            x => { x.RemoveErrorListeners(); }
        ) ?? new SignatureHelpResponse();

        return signatureHelp;
    }

    /// <summary>
    /// Gets quick info (hover)
    /// </summary>
    [JSInvokable]
    public async Task<QuickInfoResponse> GetQuickInfoAsync(
        string code,
        QuickInfoRequest quickInfoRequest)
    {
        if (!Configuration.QuickInfoEnabled)
            return new QuickInfoResponse();

        var lazyTypeResolver = HoverVisitor.CreateLazyTypeResolver(code, StepFactoryStore);
        var position         = new LinePosition(quickInfoRequest.Line, quickInfoRequest.Column);
        var command          = Helpers.GetCommand(code, position);

        if (command is null)
            return new QuickInfoResponse();

        var visitor2 = new HoverVisitor(
            command.Value.newPosition,
            command.Value.positionOffset,
            StepFactoryStore,
            lazyTypeResolver
        );

        var errorListener = new ErrorErrorListener();

        var response = visitor2.LexParseAndVisit(
            command.Value.command,
            x => { x.RemoveErrorListeners(); },
            x =>
            {
                x.RemoveErrorListeners();
                x.AddErrorListener(errorListener);
            }
        ) ?? new QuickInfoResponse();

        if (errorListener.Errors.Any())
        {
            var error = errorListener.Errors.First();
            return new QuickInfoResponse { Markdown = error.Message };
        }

        return response;
    }

    /// <summary>
    /// Sets editor diagnostics
    /// </summary>
    public async Task SetDiagnostics(MonacoEditor editor, IJSRuntime runtime)
    {
        if (!Configuration.DiagnosticsEnabled)
            return;

        var uri  = (await editor.GetModel()).Uri;
        var code = await editor.GetValue();

        var diagnostics = DiagnosticsHelper.GetDiagnostics(code, StepFactoryStore);

        await runtime.InvokeAsync<string>("setDiagnostics", diagnostics, uri);
    }
}
