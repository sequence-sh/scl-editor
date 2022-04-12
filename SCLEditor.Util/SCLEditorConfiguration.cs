namespace Reductech.Utilities.SCLEditor.Util;

/// <summary>
/// Configuration for the SCL editor
/// </summary>
public class SCLEditorConfiguration : EditorConfiguration
{
    /// <inheritdoc/>
    public override string ConfigurationKey => nameof(SCLEditorConfiguration);

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
            OnPropertyChanged(nameof(CompletionEnabled));
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
            OnPropertyChanged(nameof(SignatureHelpEnabled));
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
            OnPropertyChanged(nameof(QuickInfoEnabled));
        }
    }

    private bool _diagnosticsEnabled = false;

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
            OnPropertyChanged(nameof(DiagnosticsEnabled));
        }
    }

}
