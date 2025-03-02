using System.Text.RegularExpressions;

namespace LogSpy.Rules;

public class RegexLogRule : ILogRule
{
    private readonly Regex _regex;
    private readonly RegexOptions _regexOptions;

    public RegexLogRule(string pattern, RegexOptions regexOptions)
    {
        _regexOptions = regexOptions;
        _regex = new Regex(pattern, _regexOptions);
    }

    public bool IsViolatedBy(LogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Message))
        {
            return false;
        }

        return _regex.IsMatch(entry.Message);
    }

    public string ViolationMessage =>
        $"Message matched forbidden regex pattern '{_regex}' (regexOptions={_regexOptions}).";
}
