using LibraryCatalog.Core.Factories;
using LibraryCatalog.Core.Models;
using LibraryCatalog.Core.Samples;

namespace LibraryCatalog.Tests;

internal static class FactoryTests
{
    public static IEnumerable<(string Name, Action Test)> All()
    {
        yield return ("Factory_CreatesCompleteValidPublications", ValidObjects);
        yield return ("Factory_ProducesVariedFields", VariedFields);
        yield return ("Factory_SameSeedReproducesAllData", Reproducible);
        yield return ("Publication_TypeLabelsAreEnglish", EnglishTypeLabels);
    }

    private static void ValidObjects()
    {
        var factory = new RandomPublicationFactory(new Random(42));
        for (int id = 1; id <= 100; id++)
        {
            Publication publication = factory.Create(id);
            Check.Equal(id, publication.Id);
            Check.True(publication.Title.Length > 0 && publication.Publisher.Length > 0);
            Check.True(publication.Year is >= 1900 and <= 2026);
            Check.True(publication.GetAuthors().Count > 0);
            switch (publication)
            {
                case Book book:
                    Check.True(book.PageCount > 0 && book.Genre.Length > 0);
                    break;
                case Newspaper newspaper:
                    Check.Equal(newspaper.IssueDate.Year, newspaper.Year);
                    Check.True(newspaper.Columns.Count > 0 && newspaper.IssueNumber > 0);
                    break;
                case Almanac almanac:
                    Check.True(almanac.PageCount > 0 && almanac.Works.Count > 0);
                    break;
                default:
                    throw new InvalidOperationException("Unexpected factory result type");
            }
        }
    }

    private static void VariedFields()
    {
        var factory = new RandomPublicationFactory(new Random(17));
        var publications = Enumerable.Range(1, 100).Select(factory.Create).ToArray();
        Check.Equal(3, publications.Select(item => item.GetType()).Distinct().Count());
        Check.True(publications.Select(item => item.Title).Distinct().Count() > 3);
        Check.True(publications.Select(item => item.Publisher).Distinct().Count() > 1);
        Check.True(publications.Select(item => item.Year).Distinct().Count() > 1);
        Check.True(publications.OfType<Book>().Select(item => item.PageCount).Distinct().Count() > 1);
        Check.True(publications.SelectMany(item => item.GetAuthors()).Distinct().Count() > 1);
    }

    private static void Reproducible()
    {
        var first = new RandomPublicationFactory(new Random(123));
        var second = new RandomPublicationFactory(new Random(123));
        for (int id = 1; id <= 30; id++)
        {
            Check.Equal(Describe(first.Create(id)), Describe(second.Create(id)));
        }
    }

    private static string Describe(Publication publication)
    {
        string common = $"{publication.Id}|{publication.TypeName}|{publication.Title}|{publication.Publisher}|{publication.Year}";
        return common + (publication switch
        {
            Book book => $"|{book.Author}|{book.Genre}|{book.PageCount}",
            Newspaper newspaper => $"|{newspaper.IssueNumber}|{newspaper.IssueDate}|" +
                string.Join(";", newspaper.Columns.Select(column => $"{column.Title}:{column.Journalist}")),
            Almanac almanac => $"|{almanac.Genre}|{almanac.PageCount}|" +
                string.Join(";", almanac.Works.Select(work => $"{work.Title}:{work.Author}")),
            _ => throw new InvalidOperationException("Unexpected factory result type")
        });
    }

    private static void EnglishTypeLabels()
    {
        Check.Sequence(new[] { "Book", "Newspaper", "Almanac" },
            SampleCatalogData.CreateInitial().Select(item => item.TypeName).Distinct());
    }
}
