using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Search;

public sealed class AuthorCriterion : ISearchCriterion
{
    private readonly string _query;

    public AuthorCriterion(string query) => _query = ModelRules.RequiredText(query, "Search author");

    public bool Matches(Publication publication)
        => publication.GetAuthors().Any(author => author.Contains(_query, StringComparison.OrdinalIgnoreCase));
}
