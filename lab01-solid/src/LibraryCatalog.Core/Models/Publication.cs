namespace LibraryCatalog.Core.Models;

/// <summary>Общие данные и договор для любого издания каталога.</summary>
public abstract class Publication
{
    protected Publication(int id, string title, string publisher, int year)
    {
        Id = ModelRules.Positive(id, "ID");
        Title = ModelRules.RequiredText(title, "Title");
        Publisher = ModelRules.RequiredText(publisher, "Publisher");
        Year = ModelRules.Year(year);
    }

    public int Id { get; }
    public string Title { get; }
    public string Publisher { get; }
    public int Year { get; }
    public abstract string TypeName { get; }

    /// <summary>Возвращает авторов издания или его содержимого; операция поддерживается всеми типами.</summary>
    public abstract IReadOnlyList<string> GetAuthors();

    /// <summary>Возвращает собственное название и, для сборника, названия включённых произведений.</summary>
    public virtual IReadOnlyList<string> GetSearchTitles() => new[] { Title };
}
