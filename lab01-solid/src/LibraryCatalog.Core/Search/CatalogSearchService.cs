using LibraryCatalog.Core.Catalog;
using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Search;

public sealed class CatalogSearchService
{
    private readonly ICatalogReader _reader;

    public CatalogSearchService(ICatalogReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    public IReadOnlyList<Publication> Search(ISearchCriterion criterion)
    {
        ArgumentNullException.ThrowIfNull(criterion);
        return Array.AsReadOnly(_reader.GetAll().Where(criterion.Matches).ToArray());
    }
}
