namespace LibraryCatalog.Core.Models;

public sealed class Newspaper : Publication
{
    public Newspaper(int id, string title, string publisher, int issueNumber, DateOnly issueDate,
        IEnumerable<NewspaperColumn> columns)
        : base(id, title, publisher, issueDate.Year)
    {
        IssueNumber = ModelRules.Positive(issueNumber, "Issue number");
        IssueDate = issueDate;
        Columns = ModelRules.NonEmptyCopy(columns, "Columns");
    }

    public int IssueNumber { get; }
    public DateOnly IssueDate { get; }
    public IReadOnlyList<NewspaperColumn> Columns { get; }
    public override string TypeName => "Newspaper";
    public override IReadOnlyList<string> GetAuthors()
        => Columns.Select(column => column.Journalist).Distinct().ToArray();
}
