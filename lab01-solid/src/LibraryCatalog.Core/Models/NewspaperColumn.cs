namespace LibraryCatalog.Core.Models;

public sealed class NewspaperColumn
{
    public NewspaperColumn(string title, string journalist)
    {
        Title = ModelRules.RequiredText(title, "Column title");
        Journalist = ModelRules.RequiredText(journalist, "Journalist");
    }

    public string Title { get; }
    public string Journalist { get; }
}
