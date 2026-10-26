namespace LibraryCatalog.Core.Models;

public sealed class LiteraryWork
{
    public LiteraryWork(string title, string author)
    {
        Title = ModelRules.RequiredText(title, "Work title");
        Author = ModelRules.RequiredText(author, "Work author");
    }

    public string Title { get; }
    public string Author { get; }
}
