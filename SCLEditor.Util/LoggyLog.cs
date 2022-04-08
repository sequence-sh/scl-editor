namespace Reductech.Utilities.SCLEditor.Util;

public class LoggyLog : ILogger, INotifyPropertyChanged
{
    private readonly ILogger _logger;

    public LoggyLog(ILogger logger) => _logger = logger;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
        OnPropertyChanged(nameof(Log));
    }

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
