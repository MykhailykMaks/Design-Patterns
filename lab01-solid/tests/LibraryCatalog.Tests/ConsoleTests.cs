using LibraryCatalog.ConsoleUi;
using LibraryCatalog.Core.Catalog;
using LibraryCatalog.Core.Factories;
using LibraryCatalog.Core.Models;
using LibraryCatalog.Core.Search;

namespace LibraryCatalog.Tests;

internal static class ConsoleTests
{
    public static IEnumerable<(string Name, Action Test)> All()
    {
        yield return ("Console_ListShowsEverySpecificField", ListAll);
        yield return ("Console_GroupingShowsEveryPublicationOnce", Grouped);
        yield return ("Console_AddBook", AddBook);
        yield return ("Console_AddNewspaperWithColumns", AddNewspaper);
        yield return ("Console_AddAlmanacWithWorks", AddAlmanac);
        yield return ("Console_EditBookPreservesIdAndUnchangedFields", EditBook);
        yield return ("Console_EditNewspaperDateAndColumns", EditNewspaper);
        yield return ("Console_EditAlmanacAndWorks", EditAlmanac);
        yield return ("Console_EditBlankKeepsNestedLists", KeepNestedLists);
        yield return ("Console_DeleteAndMissingIds", DeleteAndMissingIds);
        yield return ("Console_SearchAllFourFields", SearchAllFields);
        yield return ("Console_InvalidTextNumbersAndCommandRecover", InvalidValues);
        yield return ("Console_InvalidCalendarDateRecovers", InvalidDate);
        yield return ("Console_EmptyCatalogHasUsefulOutput", EmptyCatalog);
        yield return ("Console_EndOfInputExitsAtMenu", EndAtMenu);
        yield return ("Console_EndOfInputDoesNotAddPartialBook", EndDuringAdd);
        yield return ("Console_EndOfInputPreservesEditedBook", EndDuringEdit);
        yield return ("Console_EndOfInputPreservesNestedList", EndDuringNestedEdit);
        yield return ("Console_RandomAdditionSupportsEveryType", AddRandom);
        yield return ("Console_CompleteAddEditSearchDeleteSession", CompleteSession);
        yield return ("Console_InterfaceUsesEnglishPromptsAndErrors", EnglishInterface);
        yield return ("Console_EnglishYesNoCanRetry", EnglishYesNo);
        yield return ("Console_RandomCommandUsesInjectedFactory", UsesInjectedFactory);
        yield return ("Console_GeneratedSessionHasNoRussianUi", GeneratedSessionIsEnglish);
    }

    private static (InMemoryCatalogRepository Repository, string Output) Run(string script, bool empty = false,
        Random? random = null, IRandomPublicationFactory? factory = null)
    {
        var repository = empty ? new InMemoryCatalogRepository() : CoreTests.Seeded();
        var service = new CatalogService(repository, repository);
        using var input = new StringReader(script);
        using var output = new StringWriter();
        var consoleInput = new ConsoleInput(input, output);
        var menu = new ConsoleMenu(service, new CatalogSearchService(repository), consoleInput,
            new PublicationInput(consoleInput, output), new ConsoleCatalogView(output),
            factory ?? new RandomPublicationFactory(random ?? new Random(17)));
        menu.Run();
        return (repository, output.ToString());
    }

    private static string Lines(params string[] lines) => string.Join('\n', lines) + "\n";

    private static void EnglishInterface()
    {
        var (_, output) = Run(Lines("unknown", "1", "2", "7", "4", "", "Scarlett", "5", "999", "0"));
        foreach (string expected in new[] { "LIBRARY CATALOG", "Command:", "Unknown command", "Book:", "Publisher:",
            "Author:", "Genre:", "Pages:", "Newspaper:", "Column:", "Journalist:", "Almanac:", "Work:",
            "Search field", "cannot be empty", "Found: 2", "not found", "Goodbye" })
        {
            Check.Contains(expected, output);
        }
    }

    private static void EnglishYesNo()
    {
        var (repository, output) = Run(Lines("5", "3", "", "", "", "", "maybe", "n", "0"));
        Check.Contains("Enter y or n", output);
        Check.Equal(2, ((Newspaper)repository.FindById(3)!).Columns.Count);
    }

    private static void UsesInjectedFactory()
    {
        var factory = new RecordingFactory();
        var (repository, output) = Run(Lines("4", "4", "0"), factory: factory);
        Check.Sequence(new[] { 5, 6 }, factory.RequestedIds);
        Check.Equal("Factory-created book", repository.FindById(5)!.Title);
        Check.Equal("Factory-created book", repository.FindById(6)!.Title);
        Check.Contains("Added publication with ID 6", output);
    }

    private sealed class RecordingFactory : IRandomPublicationFactory
    {
        public List<int> RequestedIds { get; } = new();

        public Publication Create(int id)
        {
            RequestedIds.Add(id);
            return CoreTests.Book(id, "Factory-created book");
        }
    }

    private static void GeneratedSessionIsEnglish()
    {
        var (_, output) = Run(Lines("unknown", "4", "4", "4", "1", "2", "5", "999", "0"), empty: true);
        Check.True(!System.Text.RegularExpressions.Regex.IsMatch(output, "[А-Яа-яЁёІіЇїЄєҐґ]"), "Non-English UI or generated data");
    }

    private static void ListAll()
    {
        var (_, output) = Run(Lines("1", "0"));
        foreach (string text in new[] { "The Lantern Keeper", "Across the Valley", "Oliver", "Mystery", "240", "Silverleaf Press", "2001",
            "The Riverside Herald", "26.10.2026", "42", "City Life", "Scarlett", "Autumn Voices", "Road to Dawn", "Matthew" })
        {
            Check.Contains(text, output);
        }
    }

    private static void Grouped()
    {
        var (_, output) = Run(Lines("2", "0"));
        Check.Contains("=== Book ===", output);
        Check.Contains("=== Newspaper ===", output);
        Check.Contains("=== Almanac ===", output);
        for (int id = 1; id <= 4; id++)
        {
            Check.Equal(1, output.Split($"[ID: {id}]").Length - 1);
        }
    }

    private static void AddBook()
    {
        var (repository, output) = Run(Lines("3", "1", "Новая книга", "Издательство", "2024", "Новый автор", "Роман", "300", "0"));
        var book = (Book)repository.FindById(5)!;
        Check.Equal("Новая книга", book.Title);
        Check.Equal("Новый автор", book.Author);
        Check.Equal(300, book.PageCount);
        Check.Contains("Added publication with ID 5", output);
    }

    private static void AddNewspaper()
    {
        var (repository, _) = Run(Lines("3", "2", "Новая газета", "Редакция", "9", "29.02.2024", "2", "Culture", "Учёный", "Город", "Журналист", "0"));
        var newspaper = (Newspaper)repository.FindById(5)!;
        Check.Equal(9, newspaper.IssueNumber);
        Check.Equal(2024, newspaper.Year);
        Check.Equal(2, newspaper.Columns.Count);
        Check.Equal("Журналист", newspaper.Columns[1].Journalist);
    }

    private static void AddAlmanac()
    {
        var (repository, _) = Run(Lines("3", "3", "Новый альманах", "Издатель", "2022", "Проза", "200", "2", "Рассказ 1", "Автор 1", "Рассказ 2", "Автор 2", "0"));
        var almanac = (Almanac)repository.FindById(5)!;
        Check.Equal("Проза", almanac.Genre);
        Check.Equal(2, almanac.Works.Count);
        Check.Equal("Рассказ 2", almanac.Works[1].Title);
    }

    private static void EditBook()
    {
        var (repository, output) = Run(Lines("5", "1", "Updated The Lantern Keeper", "", "2024", "", "", "333", "0"));
        var book = (Book)repository.FindById(1)!;
        Check.Equal("Updated The Lantern Keeper", book.Title);
        Check.Equal("Oliver", book.Author);
        Check.Equal("Silverleaf Press", book.Publisher);
        Check.Equal(2024, book.Year);
        Check.Equal(333, book.PageCount);
        Check.Contains("Publication with ID 1 updated", output);
    }

    private static void EditNewspaper()
    {
        var (repository, _) = Run(Lines("5", "3", "Новая газета", "", "99", "29.02.2024", "y", "1", "Новая колонка", "Новый журналист", "0"));
        var newspaper = (Newspaper)repository.FindById(3)!;
        Check.Equal(99, newspaper.IssueNumber);
        Check.Equal(2024, newspaper.Year);
        Check.Equal(1, newspaper.Columns.Count);
        Check.Equal("Новый журналист", newspaper.Columns[0].Journalist);
    }

    private static void EditAlmanac()
    {
        var (repository, _) = Run(Lines("5", "4", "", "", "2026", "Новый жанр", "500", "y", "1", "Новый рассказ", "Новый автор", "0"));
        var almanac = (Almanac)repository.FindById(4)!;
        Check.Equal(2026, almanac.Year);
        Check.Equal(500, almanac.PageCount);
        Check.Equal(1, almanac.Works.Count);
        Check.Equal("Новый автор", almanac.Works[0].Author);
    }

    private static void KeepNestedLists()
    {
        var (repository, _) = Run(Lines("5", "3", "", "", "", "", "", "5", "4", "", "", "", "", "", "", "0"));
        Check.Equal(2, ((Newspaper)repository.FindById(3)!).Columns.Count);
        Check.Equal(2, ((Almanac)repository.FindById(4)!).Works.Count);
    }

    private static void DeleteAndMissingIds()
    {
        var (repository, output) = Run(Lines("5", "999", "6", "999", "6", "1", "0"));
        Check.Contains("Publication with ID 999 not found", output);
        Check.True(repository.FindById(1) is null);
        Check.Equal(3, repository.GetAll().Count);
    }

    private static void SearchAllFields()
    {
        foreach (var (field, query, count) in new[] { ("1", "A QUIET MORNING", 1), ("2", "willow house", 2), ("3", "2025", 1), ("4", "scarlett", 2) })
        {
            var (repository, output) = Run(Lines("7", field, query, "0"));
            Check.Contains($"Found: {count}", output);
            Check.Equal(4, repository.GetAll().Count);
        }
    }

    private static void InvalidValues()
    {
        var (repository, output) = Run(Lines("не команда", "3", "9", "1", "", "Книга", "Издатель", "текст", "10000", "2024", "Автор", "Жанр", "0", "100", "0"));
        Check.Equal(5, repository.GetAll().Count);
        Check.Contains("Unknown command", output);
        Check.Contains("cannot be empty", output);
        Check.Contains("Enter a whole number", output);
    }

    private static void InvalidDate()
    {
        var (repository, output) = Run(Lines("3", "2", "Газета", "Редакция", "-1", "8", "31.02.2024", "29.02.2024", "0", "1", "Колонка", "Журналист", "0"));
        Check.Contains("Enter a valid date", output);
        Check.Equal(new DateOnly(2024, 2, 29), ((Newspaper)repository.FindById(5)!).IssueDate);
    }

    private static void EmptyCatalog()
    {
        var (_, output) = Run(Lines("1", "2", "7", "1", "Книга", "0"), empty: true);
        Check.Contains("The catalog is empty", output);
        Check.Contains("Found: 0", output);
    }

    private static void EndAtMenu()
    {
        var (_, output) = Run("");
        Check.Contains("Input ended", output);
    }

    private static void EndDuringAdd()
    {
        var (repository, output) = Run(Lines("3", "1", "Незаконченная книга"));
        Check.Equal(4, repository.GetAll().Count);
        Check.Contains("Input ended", output);
    }

    private static void EndDuringEdit()
    {
        var (repository, _) = Run(Lines("5", "1", "Нельзя сохранить", "Новое издательство"));
        Check.Equal("The Lantern Keeper", repository.FindById(1)!.Title);
        Check.Equal("Silverleaf Press", repository.FindById(1)!.Publisher);
    }

    private static void EndDuringNestedEdit()
    {
        var (repository, _) = Run(Lines("5", "4", "Не сохранять", "", "", "", "", "y", "2", "Рассказ", "Автор"));
        var almanac = (Almanac)repository.FindById(4)!;
        Check.Equal("Autumn Voices", almanac.Title);
        Check.Equal(2, almanac.Works.Count);
    }

    private static void AddRandom()
    {
        for (int type = 0; type < 3; type++)
        {
            var (repository, output) = Run(Lines("4", "0"), random: new CoreTests.FixedRandom(type));
            Check.Equal(5, repository.GetAll().Count);
            Check.Contains("Added publication with ID 5", output);
            Check.Equal(new[] { typeof(Book), typeof(Newspaper), typeof(Almanac) }[type], repository.FindById(5)!.GetType());
        }
    }

    private static void CompleteSession()
    {
        var (repository, output) = Run(Lines("3", "1", "Учебная книга", "Издатель", "2020", "Автор", "Роман", "100",
            "5", "5", "Обновлённая книга", "", "", "", "", "150", "7", "1", "Обновлённая", "6", "5", "1", "0"));
        Check.Contains("Added publication with ID 5", output);
        Check.Contains("Publication with ID 5 updated", output);
        Check.Contains("Found: 1", output);
        Check.Contains("Publication with ID 5 removed", output);
        Check.Equal(4, repository.GetAll().Count);
        Check.True(repository.FindById(5) is null);
    }
}
