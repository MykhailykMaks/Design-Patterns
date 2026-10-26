using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Search;

public sealed class YearCriterion : ISearchCriterion
{
    private readonly int _year;

    public YearCriterion(int year) => _year = ModelRules.Year(year);

    public bool Matches(Publication publication) => publication.Year == _year;
}
