using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Samples;

/// <summary>Вымышленные учебные записи с правдоподобными названиями и именами.</summary>
public static class SampleCatalogData
{
    public static IReadOnlyList<Publication> CreateInitial() => new Publication[]
    {
        new Book(1, "The Lantern Keeper", "Silverleaf Press", 2001, "Oliver", "Mystery", 240),
        new Book(2, "Across the Valley", "Willow House", 2002, "Tom", "Adventure", 160),
        new Newspaper(3, "The Riverside Herald", "Cedar Lane Media", 42, new DateOnly(2026, 10, 26), new[]
        {
            new NewspaperColumn("City Life", "Scarlett"),
            new NewspaperColumn("Culture", "Megan")
        }),
        new Almanac(4, "Autumn Voices", "Willow House", 2025, "Short Stories", 256, new[]
        {
            new LiteraryWork("Road to Dawn", "Matthew"),
            new LiteraryWork("A Quiet Morning", "Scarlett")
        })
    };

}
