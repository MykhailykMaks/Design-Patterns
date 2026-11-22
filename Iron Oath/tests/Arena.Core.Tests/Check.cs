using Arena.Core;

namespace Arena.Core.Tests;

internal static class Check
{
    public static void True(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message} Очікувалося: {expected}; отримано: {actual}.");
        }
    }

    public static void Succeeds(ActionResult result) => True(result.Success, result.Message);

    public static void Rejects(ActionResult result)
    {
        True(!result.Success, "Заборонена дія була прийнята.");
        True(!string.IsNullOrWhiteSpace(result.Message), "Відмова повинна пояснювати причину.");
    }

    public static void Throws<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T exception)
        {
            True(!string.IsNullOrWhiteSpace(exception.Message), "Помилка повинна пояснювати причину.");
            return;
        }

        throw new InvalidOperationException($"Очікувався виняток {typeof(T).Name}.");
    }
}
