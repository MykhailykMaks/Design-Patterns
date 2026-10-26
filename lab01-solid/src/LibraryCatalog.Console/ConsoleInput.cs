using System.Globalization;

namespace LibraryCatalog.ConsoleUi;

/// <summary>Читает и проверяет пользовательский ввод, не обращаясь к каталогу.</summary>
public sealed class ConsoleInput
{
    private readonly TextReader _reader;
    private readonly TextWriter _writer;

    public ConsoleInput(TextReader reader, TextWriter writer)
    {
        _reader = reader;
        _writer = writer;
    }

    public string ReadLine(string prompt)
    {
        _writer.Write(prompt);
        return _reader.ReadLine()?.Trim() ?? throw new EndOfStreamException();
    }

    public string ReadText(string field, string? current = null)
    {
        while (true)
        {
            string value = ReadLine(Prompt(field, current));
            if (value.Length > 0)
            {
                return value;
            }

            if (current is not null)
            {
                return current;
            }

            _writer.WriteLine("The value cannot be empty.");
        }
    }

    public int ReadInt(string field, int minimum = 1, int maximum = int.MaxValue, int? current = null)
    {
        while (true)
        {
            string value = ReadLine(Prompt(field, current?.ToString(CultureInfo.InvariantCulture)));
            if (value.Length == 0 && current.HasValue)
            {
                return current.Value;
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)
                && number >= minimum && number <= maximum)
            {
                return number;
            }

            _writer.WriteLine($"Enter a whole number from {minimum} to {maximum}.");
        }
    }

    public DateOnly ReadDate(string field, DateOnly? current = null)
    {
        while (true)
        {
            string value = ReadLine(Prompt(field + " (DD.MM.YYYY)", current?.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)));
            if (value.Length == 0 && current.HasValue)
            {
                return current.Value;
            }

            if (DateOnly.TryParseExact(value, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            _writer.WriteLine("Enter a valid date in DD.MM.YYYY format, for example 26.10.2026.");
        }
    }

    public bool ReadYesNo(string prompt)
    {
        while (true)
        {
            string answer = ReadLine(prompt).ToLowerInvariant();
            if (answer is "y" or "yes")
            {
                return true;
            }

            if (answer is "" or "n" or "no")
            {
                return false;
            }

            _writer.WriteLine("Enter y or n; Enter means no.");
        }
    }

    private static string Prompt(string field, string? current)
        => current is null ? $"{field}: " : $"{field} [{current}], Enter to keep: ";
}
