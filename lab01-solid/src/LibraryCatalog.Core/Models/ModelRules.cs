namespace LibraryCatalog.Core.Models;

internal static class ModelRules
{
    public static string RequiredText(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Field [{field}] cannot be empty.");
        }

        return value.Trim();
    }

    public static int Positive(int value, string field)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(field, $"Field [{field}] must be positive.");
        }

        return value;
    }

    public static int Year(int value)
    {
        if (value is < 1 or > 9999)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "The year must be between 1 and 9999.");
        }

        return value;
    }

    public static IReadOnlyList<T> NonEmptyCopy<T>(IEnumerable<T> source, string field) where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        var items = source.ToArray();
        if (items.Length == 0 || items.Any(item => item is null))
        {
            throw new ArgumentException($"List [{field}] must contain at least one non-null item.");
        }

        return Array.AsReadOnly(items);
    }
}
