namespace LibraryCatalog.Core.Models;

public sealed class Almanac : Publication
{
    public Almanac(int id, string title, string publisher, int year, string genre, int pageCount,
        IEnumerable<LiteraryWork> works)
        : base(id, title, publisher, year)
    {
        Genre = ModelRules.RequiredText(genre, "Genre");
        PageCount = ModelRules.Positive(pageCount, "Page count");
        Works = ModelRules.NonEmptyCopy(works, "Works");
    }

    public string Genre { get; }
    public int PageCount { get; }
    public IReadOnlyList<LiteraryWork> Works { get; }
    public override string TypeName => "Almanac";
    public override IReadOnlyList<string> GetAuthors()
        => Works.Select(work => work.Author).Distinct().ToArray();

    public override IReadOnlyList<string> GetSearchTitles()
        => new[] { Title }.Concat(Works.Select(work => work.Title)).ToArray();
}
