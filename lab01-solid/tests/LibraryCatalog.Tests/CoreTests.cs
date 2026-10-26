using LibraryCatalog.Core.Catalog;
using LibraryCatalog.Core.Factories;
using LibraryCatalog.Core.Models;
using LibraryCatalog.Core.Samples;
using LibraryCatalog.Core.Search;

namespace LibraryCatalog.Tests;

internal static class CoreTests
{
    public static IEnumerable<(string Name, Action Test)> All()
    {
        yield return ("Book_NormalizesRequiredText", BookNormalizesText);
        yield return ("Models_RejectInvalidFields", ModelsRejectInvalidFields);
        yield return ("Publication_AllTypesHonorCommonContract", AllTypesHonorCommonContract);
        yield return ("NestedCollections_AreDefensiveCopies", NestedCollectionsAreCopies);
        yield return ("Catalog_AddUpdateRemoveAndMissingId", CatalogCrud);
        yield return ("Catalog_DuplicateDoesNotOverwrite", DuplicateDoesNotOverwrite);
        yield return ("Catalog_UpdateCannotChangeType", UpdateCannotChangeType);
        yield return ("Catalog_InvalidReplacementPreservesOriginal", InvalidReplacementPreservesOriginal);
        yield return ("Catalog_ReturnsOrderedSnapshot", OrderedSnapshot);
        yield return ("Catalog_IdAllocationDoesNotReuseOrCollide", UniqueIds);
        yield return ("Search_TitleIncludesNestedWorksWithoutDuplicates", SearchTitles);
        yield return ("Search_PublisherIgnoresCaseAndTrims", SearchPublishers);
        yield return ("Search_YearMatchesExactly", SearchYear);
        yield return ("Search_AuthorsIncludeJournalistsAndAlmanacAuthors", SearchAuthors);
        yield return ("Search_RejectsEmptyQueriesAndInvalidYear", InvalidQueries);
        yield return ("Search_NewCriterionNeedsNoServiceChanges", NewCriterion);
        yield return ("Samples_ContainAllTypesAndNestedFields", InitialSamples);
        yield return ("Factory_ControlledRandomReachesAllTypes", RandomTypes);
        yield return ("Catalog_EmptyOperationsHaveDefinedResults", EmptyCatalog);
    }

    internal static Book Book(int id = 1, string title = "Книга", string author = "Автор", int year = 2020)
        => new(id, title, "Видавництво", year, author, "Роман", 120);

    private static void BookNormalizesText()
    {
        var book = new Book(1, " Книга ", " Издатель ", 2020, " Автор ", " Роман ", 120);
        Check.Equal("Книга", book.Title);
        Check.Equal("Автор", book.Author);
        Check.Equal("Издатель", book.Publisher);
        Check.Equal("Роман", book.Genre);
    }

    private static void ModelsRejectInvalidFields()
    {
        Check.Throws<ArgumentException>(() => Book(title: "  "));
        Check.Throws<ArgumentException>(() => Book(author: ""));
        Check.Throws<ArgumentException>(() => Book(id: 0));
        Check.Throws<ArgumentException>(() => Book(year: 0));
        Check.Throws<ArgumentException>(() => Book(year: 10000));
        Check.Throws<ArgumentException>(() => new Book(1, "A", "P", 2020, "B", "G", 0));
        Check.Throws<ArgumentException>(() => new NewspaperColumn("", "A"));
        Check.Throws<ArgumentException>(() => new LiteraryWork("A", ""));
        Check.Throws<ArgumentException>(() => new Newspaper(1, "N", "P", 0, new DateOnly(2020, 1, 1), new[] { new NewspaperColumn("C", "A") }));
        Check.Throws<ArgumentException>(() => new Newspaper(1, "N", "P", 1, new DateOnly(2020, 1, 1), Array.Empty<NewspaperColumn>()));
        Check.Throws<ArgumentException>(() => new Almanac(1, "A", "P", 2020, "G", 10, Array.Empty<LiteraryWork>()));
        Check.Throws<ArgumentException>(() => new Almanac(1, "A", "P", 2020, "G", 10, new LiteraryWork[] { null! }));
    }

    private static void AllTypesHonorCommonContract()
    {
        var publications = SampleCatalogData.CreateInitial();
        foreach (Publication publication in publications)
        {
            Check.True(publication.Id > 0);
            Check.True(!string.IsNullOrWhiteSpace(publication.Title));
            Check.True(!string.IsNullOrWhiteSpace(publication.Publisher));
            Check.True(publication.Year is >= 1 and <= 9999);
            Check.True(publication.GetAuthors().Count > 0);
            Check.True(publication.GetAuthors().All(author => !string.IsNullOrWhiteSpace(author)));
            Check.True(publication.GetSearchTitles().Contains(publication.Title));
            Check.True(!string.IsNullOrWhiteSpace(publication.TypeName));
        }

        var newspaper = publications.OfType<Newspaper>().Single();
        Check.Equal(newspaper.IssueDate.Year, newspaper.Year);
    }

    private static void NestedCollectionsAreCopies()
    {
        var works = new List<LiteraryWork> { new("Твір", "Автор") };
        var almanac = new Almanac(1, "Альманах", "Издатель", 2020, "Short Stories", 100, works);
        works.Clear();
        Check.Equal(1, almanac.Works.Count);
        Check.Throws<NotSupportedException>(() => ((IList<LiteraryWork>)almanac.Works).Clear());

        var columns = new List<NewspaperColumn> { new("City Life", "Автор") };
        var newspaper = new Newspaper(2, "Газета", "Редакция", 7, new DateOnly(2026, 10, 26), columns);
        columns.Clear();
        Check.Equal(1, newspaper.Columns.Count);
        Check.Throws<NotSupportedException>(() => ((IList<NewspaperColumn>)newspaper.Columns).Clear());
    }

    private static void CatalogCrud()
    {
        var repository = new InMemoryCatalogRepository();
        var service = new CatalogService(repository, repository);
        service.Add(Book());
        Check.Equal("Книга", service.FindById(1)!.Title);
        Check.True(service.Update(Book(title: "Обновлённая")));
        Check.Equal("Обновлённая", service.FindById(1)!.Title);
        Check.True(!service.Update(Book(999)));
        Check.True(!service.Remove(999));
        Check.True(service.Remove(1));
        Check.Equal(0, service.GetAll().Count);
    }

    private static void DuplicateDoesNotOverwrite()
    {
        var repository = new InMemoryCatalogRepository();
        repository.Add(Book());
        Check.Throws<InvalidOperationException>(() => repository.Add(Book(title: "Другая")));
        Check.Equal("Книга", repository.FindById(1)!.Title);
    }

    private static void UpdateCannotChangeType()
    {
        var repository = new InMemoryCatalogRepository();
        var service = new CatalogService(repository, repository);
        service.Add(Book());
        var replacement = new Almanac(1, "Альманах", "P", 2020, "G", 10, new[] { new LiteraryWork("W", "A") });
        Check.Throws<InvalidOperationException>(() => service.Update(replacement));
        Check.True(service.FindById(1) is Book);
    }

    private static void InvalidReplacementPreservesOriginal()
    {
        var repository = new InMemoryCatalogRepository();
        var service = new CatalogService(repository, repository);
        var original = Book();
        service.Add(original);
        Check.Throws<ArgumentException>(() => service.Update(Book(title: " ")));
        Check.True(ReferenceEquals(original, service.FindById(1)));
    }

    private static void OrderedSnapshot()
    {
        var repository = new InMemoryCatalogRepository();
        repository.Add(Book(9));
        repository.Add(Book(2));
        var snapshot = repository.GetAll();
        Check.Sequence(new[] { 2, 9 }, snapshot.Select(item => item.Id));
        repository.Remove(2);
        Check.Equal(2, snapshot.Count);
        Check.Equal(1, repository.GetAll().Count);
    }

    private static void UniqueIds()
    {
        var repository = new InMemoryCatalogRepository();
        var service = new CatalogService(repository, repository);
        service.Add(Book(10));
        Check.Equal(11, service.NextId());
        service.Remove(10);
        Check.Equal(12, service.NextId());
        repository.Add(Book(50));
        Check.Equal(51, service.NextId());
    }

    internal static InMemoryCatalogRepository Seeded()
    {
        var repository = new InMemoryCatalogRepository();
        foreach (var publication in SampleCatalogData.CreateInitial())
        {
            repository.Add(publication);
        }

        return repository;
    }

    private static void SearchTitles()
    {
        var repository = new InMemoryCatalogRepository();
        repository.Add(new Almanac(1, "A Quiet Morning", "P", 2020, "G", 10,
            new[] { new LiteraryWork("A Quiet Morning version 1", "A"), new LiteraryWork("A Quiet Morning version 2", "A") }));
        var search = new CatalogSearchService(repository);
        Check.Equal(1, search.Search(new TitleCriterion(" A QUIET MORNING ")).Count);
    }

    private static void SearchPublishers()
    {
        var search = new CatalogSearchService(Seeded());
        Check.Sequence(new[] { 2, 4 }, search.Search(new PublisherCriterion(" WILLOW HOUSE ")).Select(item => item.Id));
    }

    private static void SearchYear()
    {
        var search = new CatalogSearchService(Seeded());
        Check.Sequence(new[] { 4 }, search.Search(new YearCriterion(2025)).Select(item => item.Id));
        Check.Equal(0, search.Search(new YearCriterion(1900)).Count);
    }

    private static void SearchAuthors()
    {
        var search = new CatalogSearchService(Seeded());
        Check.Sequence(new[] { 3, 4 }, search.Search(new AuthorCriterion(" scarlett ")).Select(item => item.Id));
        Check.Sequence(new[] { 1 }, search.Search(new AuthorCriterion("OLIVER")).Select(item => item.Id));
    }

    private static void InvalidQueries()
    {
        Check.Throws<ArgumentException>(() => new TitleCriterion(" "));
        Check.Throws<ArgumentException>(() => new PublisherCriterion(""));
        Check.Throws<ArgumentException>(() => new AuthorCriterion("\t"));
        Check.Throws<ArgumentException>(() => new YearCriterion(0));
    }

    private static void NewCriterion()
    {
        var search = new CatalogSearchService(Seeded());
        Check.Sequence(new[] { 3, 4 }, search.Search(new SinceYearCriterion(2025)).Select(item => item.Id));
    }

    private sealed class SinceYearCriterion(int year) : ISearchCriterion
    {
        public bool Matches(Publication publication) => publication.Year >= year;
    }

    private static void InitialSamples()
    {
        var samples = SampleCatalogData.CreateInitial();
        Check.Equal(samples.Count, samples.Select(item => item.Id).Distinct().Count());
        Check.True(samples.OfType<Book>().Any());
        Check.True(samples.OfType<Newspaper>().Single().Columns.Count >= 2);
        Check.True(samples.OfType<Almanac>().Single().Works.Select(work => work.Author).Distinct().Count() >= 2);
    }

    private static void RandomTypes()
    {
        Check.True(new RandomPublicationFactory(new FixedRandom(0)).Create(10) is Book);
        Check.True(new RandomPublicationFactory(new FixedRandom(1)).Create(11) is Newspaper);
        Check.True(new RandomPublicationFactory(new FixedRandom(2)).Create(12) is Almanac);
        var repository = Seeded();
        var service = new CatalogService(repository, repository);
        for (int index = 0; index < 30; index++)
        {
            service.Add(new RandomPublicationFactory(new FixedRandom(index % 3)).Create(service.NextId()));
        }

        Check.Equal(34, service.GetAll().Count);
        Check.Equal(34, service.GetAll().Select(item => item.Id).Distinct().Count());
    }

    private static void EmptyCatalog()
    {
        var repository = new InMemoryCatalogRepository();
        Check.Equal(0, repository.GetAll().Count);
        Check.True(repository.FindById(7) is null);
        Check.True(!repository.Update(Book(7)));
        Check.True(!repository.Remove(7));
        Check.Equal(0, new CatalogSearchService(repository).Search(new TitleCriterion("A")).Count);
    }

    internal sealed class FixedRandom(int value) : Random
    {
        public override int Next(int maxValue) => value % maxValue;
    }
}
