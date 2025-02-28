namespace LogSpy;

public static class CorrelationContext
{
    private static readonly AsyncLocal<string> _currentId = new();

    /// <summary>
    /// Gets or sets the current correlation (or test) ID in this async flow.
    /// </summary>
    public static string CurrentId
    {
        get => _currentId.Value;
        set => _currentId.Value = value;
    }

    /// <summary>
    /// Sets the CorrelationId for the current async context,
    /// and returns a disposable that resets it upon disposal.
    /// </summary>
    public static IDisposable BeginCorrelationScope(string correlationId)
    {
        var oldId = _currentId.Value;
        _currentId.Value = correlationId;

        return new CorrelationScope(() =>
        {
            // Reset to old ID
            _currentId.Value = oldId;
        });
    }

    private class CorrelationScope : IDisposable
    {
        private readonly Action _onDispose;
        public CorrelationScope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}
