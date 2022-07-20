using JetBrains.Annotations;

namespace Reductech.Utilities.SCLEditor.Components;

/// <summary>
/// The editor configuration
/// </summary>
public class EditorConfiguration : INotifyPropertyChanged
{
    /// <summary>
    /// The configuration key in local storage
    /// </summary>
    public virtual string ConfigurationKey => nameof(EditorConfiguration);

    private bool _minimapEnabled;

    /// <summary>
    /// Whether the minimap view is enabled
    /// </summary>
    public bool MinimapEnabled
    {
        get => _minimapEnabled;
        set
        {
            if (value == _minimapEnabled)
                return;

            _minimapEnabled = value;
            OnPropertyChanged(nameof(MinimapEnabled));
        }
    }

    private bool _completionEnabled = true;

    /// <summary>
    /// Whether code completion is enabled
    /// </summary>
    public virtual bool CompletionEnabled
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
    public virtual bool SignatureHelpEnabled
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
    public virtual bool QuickInfoEnabled
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

    private bool _diagnosticsEnabled;

    /// <summary>
    /// Whether diagnostics is enabled
    /// </summary>
    public virtual bool DiagnosticsEnabled
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

    private bool _readOnly;

    /// <summary>
    /// Whether this editor is readonly
    /// </summary>
    public bool ReadOnly
    {
        get => _readOnly;
        set
        {
            if (value == _readOnly)
                return;

            _readOnly = value;
            OnPropertyChanged(nameof(ReadOnly));
        }
    }

    /// <summary>
    /// Called whenever a configuration property is changed
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// To be called whenever a property changes
    /// </summary>
    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
