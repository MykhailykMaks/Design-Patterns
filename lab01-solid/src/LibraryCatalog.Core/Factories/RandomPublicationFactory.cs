using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Factories;

/// <summary>Простая фабрика: выбор типа и заполнение данных скрыты от меню.</summary>
public sealed class RandomPublicationFactory : IRandomPublicationFactory
{
    private readonly Random _random;

    private static readonly string[] Authors =
        { "Oliver", "Tom", "Scarlett", "Megan", "Matthew", "Alex" };
    private static readonly string[] Publishers =
        { "Silverleaf Press", "Willow House", "Cedar Lane Media", "Amberfield Books" };
    private static readonly string[] BookTitles =
        { "The Lantern Keeper", "Across the Valley", "Letters from Elm Street", "Beyond the Old Bridge", "The Winter Orchard", "A Map of Tomorrow" };
    private static readonly string[] Genres = { "Mystery", "Adventure", "Short Stories", "Poetry", "Drama" };
    private static readonly string[] NewspaperTitles = { "The Riverside Herald", "The Morning Chronicle", "Oakfield Gazette", "The Weekly Beacon" };
    private static readonly string[] ColumnTitles = { "City Life", "Culture", "Science", "Sports", "Education" };
    private static readonly string[] AlmanacTitles = { "Autumn Voices", "Stories by the River", "Echoes of Spring", "The Turning Season" };
    private static readonly string[] WorkTitles = { "Road to Dawn", "A Quiet Morning", "The Paper Boat", "Summer Rain", "The Empty Station", "One More Letter" };

    public RandomPublicationFactory(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);
        _random = random;
    }

    public Publication Create(int id)
    {
        return _random.Next(3) switch
        {
            0 => CreateBook(id),
            1 => CreateNewspaper(id),
            _ => CreateAlmanac(id)
        };
    }

    private Book CreateBook(int id)
        => new(id, Pick(BookTitles), Pick(Publishers), _random.Next(1900, 2027),
            Pick(Authors), Pick(Genres), _random.Next(80, 601));

    private Newspaper CreateNewspaper(int id)
    {
        int year = _random.Next(1900, 2027);
        int month = _random.Next(1, 13);
        int day = _random.Next(1, DateTime.DaysInMonth(year, month) + 1);
        var columns = new List<NewspaperColumn>();
        int count = _random.Next(1, 4);
        for (int index = 0; index < count; index++)
        {
            columns.Add(new NewspaperColumn(Pick(ColumnTitles), Pick(Authors)));
        }

        return new Newspaper(id, Pick(NewspaperTitles), Pick(Publishers), _random.Next(1, 366),
            new DateOnly(year, month, day), columns);
    }

    private Almanac CreateAlmanac(int id)
    {
        var works = new List<LiteraryWork>();
        int count = _random.Next(2, 5);
        for (int index = 0; index < count; index++)
        {
            works.Add(new LiteraryWork(Pick(WorkTitles), Pick(Authors)));
        }

        return new Almanac(id, Pick(AlmanacTitles), Pick(Publishers), _random.Next(1900, 2027),
            Pick(Genres), _random.Next(100, 501), works);
    }

    private string Pick(string[] values) => values[_random.Next(values.Length)];
}
