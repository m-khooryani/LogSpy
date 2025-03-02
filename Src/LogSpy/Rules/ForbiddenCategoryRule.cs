namespace LogSpy.Rules;

public class ForbiddenCategoryRule : ILogRule
{
    private readonly List<string> _forbiddenCategories;

    public ForbiddenCategoryRule(params string[] categories)
    {
        _forbiddenCategories = categories?.ToList() ?? new List<string>();
    }

    public bool IsViolatedBy(LogEntry entry)
    {
        return _forbiddenCategories.Any(fc =>
            entry.Category?.StartsWith(fc, StringComparison.OrdinalIgnoreCase) == true);
    }

    public string ViolationMessage =>
        $"Category is forbidden. Forbidden categories: [{string.Join(", ", _forbiddenCategories)}].";
}
