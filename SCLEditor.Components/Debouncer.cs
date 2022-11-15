namespace Sequence.Utilities.SCLEditor.Components;

/// <summary>
/// Provides debounce functionality
/// </summary>
public class Debouncer
{
    /// <summary>
    /// Create a new debouncer
    /// </summary>
    public Debouncer(TimeSpan interval)
    {
        Interval = interval;
    }

    /// <summary>
    /// The interval to wait before actions will be performed
    /// </summary>
    public TimeSpan Interval { get; }

    private Timer? _timer;

    /// <summary>
    /// Queue an action to be dispatched
    /// </summary>
    public void Dispatch(Action action)
    {
        _timer?.Dispose();

        _timer = new Timer(
            delegate(object? state)
            {
                _timer?.Dispose();

                if (state is Action a)
                    a.Invoke();
            },
            action,
            Interval,
            Timeout.InfiniteTimeSpan
        );
    }
}
