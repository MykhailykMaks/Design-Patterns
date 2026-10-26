using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Catalog;

public interface ICatalogWriter
{
    void Add(Publication publication);
    bool Update(Publication publication);
    bool Remove(int id);
}
