using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Search;

public interface ISearchCriterion
{
    bool Matches(Publication publication);
}
