using System.Collections.Concurrent;

namespace LogSpy;

public class LogCaptureService
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();
    private readonly List<ILogRule> _rules = [];
    private readonly List<string> _violations = [];

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
        var violations = CheckRules(entry);

        HandleViolations(violations);

        _entries.Enqueue(entry);
    }

    private void HandleViolations(IReadOnlyList<string> violations)
    {
        if (!violations.Any())
        {
            return;
        }

        switch (Mode)
        {
            case RuleViolationMode.ImmediateFail:
                {
                    var combinedMsg = string.Join(Environment.NewLine, violations);
                    throw new InvalidOperationException($"Immediate rule violation: {combinedMsg}");
                }

            case RuleViolationMode.DeferredFail:
            default:
                lock (_violations)
                {
                    _violations.AddRange(violations);
                }
                break;
        }
    }

    private IReadOnlyList<string> CheckRules(LogEntry entry)
    {
        List<string> localViolations = new();
        lock (_rules)
        {
            foreach (var rule in _rules)
            {
                if (rule.IsViolatedBy(entry))
                {
                    localViolations.Add(rule.ViolationMessage);
                }
            }
        }

        return localViolations;
    }
}
