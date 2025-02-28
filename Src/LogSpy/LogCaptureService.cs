using System.Collections.Concurrent;

namespace LogSpy;

public class LogCaptureService
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly List<ILogRule> _rules = new();
    private readonly List<string> _violations = new();

    public RuleViolationMode Mode { get; set; } = RuleViolationMode.DeferredFail;

    public IReadOnlyCollection<LogEntry> Entries => _entries.ToArray();
    public IReadOnlyList<string> Violations => _violations;

    public void AddRule(ILogRule rule)
    {
        lock (_rules)
        {
            _rules.Add(rule);
        }
    }

    public void Clear()
    {
        while (_entries.TryDequeue(out _)) { }
        lock (_rules) { _rules.Clear(); }
        lock (_violations) { _violations.Clear(); }
    }

    public void AddEntry(LogEntry entry)
    {
        // 1) Check each rule
        List<string> localViolations = null;
        lock (_rules)
        {
            foreach (var rule in _rules)
            {
                if (rule.IsViolatedBy(entry))
                {
                    localViolations ??= new List<string>();
                    localViolations.Add(rule.ViolationMessage);
                }
            }
        }

        // 2) Handle any violations depending on Mode
        if (localViolations != null)
        {
            if (Mode == RuleViolationMode.ImmediateFail)
            {
                // Throw an exception right away (stop the test)
                var combinedMsg = string.Join(Environment.NewLine, localViolations);
                throw new InvalidOperationException(
                    $"Immediate rule violation: {combinedMsg}"
                );
            }
            else
            {
                // Deferred: store them for later
                lock (_violations)
                {
                    _violations.AddRange(localViolations);
                }
            }
        }

        // 3) Finally enqueue the log entry
        _entries.Enqueue(entry);
    }
}
