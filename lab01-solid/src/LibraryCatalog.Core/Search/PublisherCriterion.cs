using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Search;

public sealed class PublisherCriterion : ISearchCriterion
{
    private readonly string _query;

    public PublisherCriterion(string query) => _query = ModelRules.RequiredText(query, "Search publisher");

    public bool Matches(Publication publication)
        => publication.Publisher.Contains(_query, StringComparison.OrdinalIgnoreCase);
}
