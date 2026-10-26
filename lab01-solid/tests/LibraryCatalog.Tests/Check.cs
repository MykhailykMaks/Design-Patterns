namespace LibraryCatalog.Tests;

internal static class Check
{
    public static void True(bool value, string message = "Expected true")
    {
        if (!value)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual)
    {
        True(EqualityComparer<T>.Default.Equals(expected, actual), $"Expected '{expected}', got '{actual}'");
    }

    public static void Sequence<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        True(expected.SequenceEqual(actual), "Sequences differ");
    }

    public static void Contains(string expected, string actual)
    {
        True(actual.Contains(expected, StringComparison.Ordinal), $"Missing text: {expected}");
    }

    public static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }

        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
}
