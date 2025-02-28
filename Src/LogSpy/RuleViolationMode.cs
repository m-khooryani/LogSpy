namespace LogSpy;

public enum RuleViolationMode
{
    /// <summary>
    /// Store the violation in a collection; user checks and fails at the end.
    /// </summary>
    DeferredFail,

    /// <summary>
    /// Immediately throw an exception when a log violates a rule.
    /// </summary>
    ImmediateFail
}
