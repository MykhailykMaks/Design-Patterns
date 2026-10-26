using LibraryCatalog.Core.Models;

namespace LibraryCatalog.ConsoleUi;

/// <summary>Запрашивает данные издания и создаёт полностью проверенный объект.</summary>
public sealed class PublicationInput
{
    private readonly ConsoleInput _input;
    private readonly TextWriter _writer;

    public PublicationInput(ConsoleInput input, TextWriter writer)
    {
        _input = input;
        _writer = writer;
    }

    public Publication Create(int id, int type)
    {
        return type switch
        {
            1 => ReadBook(id),
            2 => ReadNewspaper(id),
            3 => ReadAlmanac(id),
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Unknown publication type.")
        };
    }

    public Publication Edit(Publication publication)
    {
        _writer.WriteLine("Press Enter to keep the current value. ID and type stay unchanged.");
        return publication switch
        {
            Book book => ReadBook(book.Id, book),
            Newspaper newspaper => ReadNewspaper(newspaper.Id, newspaper),
            Almanac almanac => ReadAlmanac(almanac.Id, almanac),
            _ => throw new ArgumentException("No edit form is available for this publication type.")
        };
    }

    private Book ReadBook(int id, Book? current = null)
    {
        string title = _input.ReadText("Title", current?.Title);
        string publisher = _input.ReadText("Publisher", current?.Publisher);
        int year = _input.ReadInt("Publication year", 1, 9999, current?.Year);
        string author = _input.ReadText("Author", current?.Author);
        string genre = _input.ReadText("Genre", current?.Genre);
        int pages = _input.ReadInt("Page count", current: current?.PageCount);
        return new Book(id, title, publisher, year, author, genre, pages);
    }

    private Newspaper ReadNewspaper(int id, Newspaper? current = null)
    {
        string title = _input.ReadText("Title", current?.Title);
        string publisher = _input.ReadText("Publisher / editorial office", current?.Publisher);
        int number = _input.ReadInt("Issue number", current: current?.IssueNumber);
        DateOnly date = _input.ReadDate("Issue date", current?.IssueDate);
        IReadOnlyList<NewspaperColumn> columns = current?.Columns ?? Array.Empty<NewspaperColumn>();
        if (current is null || _input.ReadYesNo("Replace the column list? (y/n, Enter - no): "))
        {
            columns = ReadColumns();
        }

        return new Newspaper(id, title, publisher, number, date, columns);
    }

    private Almanac ReadAlmanac(int id, Almanac? current = null)
    {
        string title = _input.ReadText("Almanac title", current?.Title);
        string publisher = _input.ReadText("Publisher", current?.Publisher);
        int year = _input.ReadInt("Publication year", 1, 9999, current?.Year);
        string genre = _input.ReadText("Genre", current?.Genre);
        int pages = _input.ReadInt("Page count", current: current?.PageCount);
        IReadOnlyList<LiteraryWork> works = current?.Works ?? Array.Empty<LiteraryWork>();
        if (current is null || _input.ReadYesNo("Replace the work list? (y/n, Enter - no): "))
        {
            works = ReadWorks();
        }

        return new Almanac(id, title, publisher, year, genre, pages, works);
    }

    private IReadOnlyList<NewspaperColumn> ReadColumns()
    {
        int count = _input.ReadInt("Number of columns");
        var columns = new List<NewspaperColumn>();
        for (int index = 1; index <= count; index++)
        {
            _writer.WriteLine($"Column {index}:");
            string title = _input.ReadText("  Column title");
            string journalist = _input.ReadText("  Journalist");
            columns.Add(new NewspaperColumn(title, journalist));
        }

        return columns;
    }

    private IReadOnlyList<LiteraryWork> ReadWorks()
    {
        int count = _input.ReadInt("Number of works");
        var works = new List<LiteraryWork>();
        for (int index = 1; index <= count; index++)
        {
            _writer.WriteLine($"Work {index}:");
            string title = _input.ReadText("  Work / book title");
            string author = _input.ReadText("  Work author");
            works.Add(new LiteraryWork(title, author));
        }

        return works;
    }
}
