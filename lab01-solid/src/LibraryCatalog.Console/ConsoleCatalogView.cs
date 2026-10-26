using System.Globalization;
using LibraryCatalog.Core.Models;

namespace LibraryCatalog.ConsoleUi;

/// <summary>Только оформление вывода; не изменяет каталог.</summary>
public sealed class ConsoleCatalogView
{
    private readonly TextWriter _writer;

    public ConsoleCatalogView(TextWriter writer) => _writer = writer;

    public void ShowMessage(string message) => _writer.WriteLine(message);

    public void ShowMenu()
    {
        _writer.WriteLine();
        _writer.WriteLine("1 - Show all publications");
        _writer.WriteLine("2 - Group publications by type");
        _writer.WriteLine("3 - Add a publication manually");
        _writer.WriteLine("4 - Generate a random publication");
        _writer.WriteLine("5 - Edit a publication");
        _writer.WriteLine("6 - Remove a publication");
        _writer.WriteLine("7 - Search publications");
        _writer.WriteLine("0 - Exit");
    }

    public void ShowAll(IReadOnlyList<Publication> publications, bool grouped = false)
    {
        if (publications.Count == 0)
        {
            _writer.WriteLine("The catalog is empty.");
            return;
        }

        if (grouped)
        {
            foreach (var group in publications.GroupBy(item => item.TypeName))
            {
                _writer.WriteLine($"=== {group.Key} ===");
                foreach (var publication in group.OrderBy(item => item.Id))
                {
                    ShowPublication(publication);
                }
            }
        }
        else
        {
            foreach (var publication in publications.OrderBy(item => item.Id))
            {
                ShowPublication(publication);
            }
        }

        _writer.WriteLine($"Total publications: {publications.Count}");
    }

    public void ShowSearchResults(IReadOnlyList<Publication> publications)
    {
        _writer.WriteLine($"Found: {publications.Count}");
        if (publications.Count > 0)
        {
            ShowAll(publications);
        }
    }

    public void ShowPublication(Publication publication)
    {
        _writer.WriteLine($"[ID: {publication.Id}] {publication.TypeName}: {publication.Title}");
        _writer.WriteLine($"  Publisher: {publication.Publisher}; Year: {publication.Year}");
        switch (publication)
        {
            case Book book:
                _writer.WriteLine($"  Author: {book.Author}; Genre: {book.Genre}; Pages: {book.PageCount}");
                break;
            case Newspaper newspaper:
                _writer.WriteLine($"  Issue: {newspaper.IssueNumber}; Date: {newspaper.IssueDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)}");
                foreach (var column in newspaper.Columns)
                {
                    _writer.WriteLine($"  Column: {column.Title}; Journalist: {column.Journalist}");
                }

                break;
            case Almanac almanac:
                _writer.WriteLine($"  Genre: {almanac.Genre}; Pages: {almanac.PageCount}");
                foreach (var work in almanac.Works)
                {
                    _writer.WriteLine($"  Work: {work.Title}; Author: {work.Author}");
                }

                break;
        }
    }
}
