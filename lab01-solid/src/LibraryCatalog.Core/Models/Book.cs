namespace LibraryCatalog.Core.Models;

public sealed class Book : Publication
{
    public Book(int id, string title, string publisher, int year, string author, string genre, int pageCount)
        : base(id, title, publisher, year)
    {
        Author = ModelRules.RequiredText(author, "Author");
        Genre = ModelRules.RequiredText(genre, "Genre");
        PageCount = ModelRules.Positive(pageCount, "Page count");
    }

    public string Author { get; }
    public string Genre { get; }
    public int PageCount { get; }
    public override string TypeName => "Book";
    public override IReadOnlyList<string> GetAuthors() => new[] { Author };
}
