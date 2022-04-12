namespace Reductech.Utilities.SCLEditor.Util;

public class EditorConfiguration
{
    /// <summary>
    /// The configuration key in local storage
    /// </summary>
    public virtual string ConfigurationKey => nameof(EditorConfiguration);

    private bool _minimapEnabled = false;

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

    /// <summary>
    /// Called whenever a configuration property is changed
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
