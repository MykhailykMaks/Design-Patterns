using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Search;

public sealed class TitleCriterion : ISearchCriterion
{
    private readonly string _query;

    public TitleCriterion(string query) => _query = ModelRules.RequiredText(query, "Search title");

    public bool Matches(Publication publication)
        => publication.GetSearchTitles().Any(title => title.Contains(_query, StringComparison.OrdinalIgnoreCase));
}
