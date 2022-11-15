using JetBrains.Annotations;
using Sequence.Core.Internal.Logging;

namespace Reductech.Utilities.SCLEditor.Components;

public class ObservableLogger : ILogger, INotifyPropertyChanged
{
    private readonly ILogger _logger;

    public ObservableLogger(ILogger logger) => _logger = logger;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var logMessage = state as LogMessage;

        if (logMessage == null)
        {
            _logger.Log(logLevel, eventId, state, exception, formatter);
        }
        else
        {
            var lvlString = logLevel.ToString().ToUpper();
            var lvl       = lvlString.Length > 5 ? lvlString.Substring(0, 4) : lvlString;

            var message =
                $"{logMessage.DateTime:yyyy/MM/dd HH:mm:ss}  {lvl,-6} {logMessage.Message}";

            _logger.Log(logLevel, eventId, message);
        }

        OnPropertyChanged(nameof(Log));
    }

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

    public event PropertyChangedEventHandler? PropertyChanged;

    [NotifyPropertyChangedInvocator]
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
