using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Catalog;

/// <summary>Только чтение каталога: потребителям поиска не нужны операции записи.</summary>
public interface ICatalogReader
{
    IReadOnlyList<Publication> GetAll();
    Publication? FindById(int id);
}
