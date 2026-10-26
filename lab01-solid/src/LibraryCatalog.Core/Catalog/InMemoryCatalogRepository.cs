using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Catalog;

/// <summary>Хранит издания только в памяти текущего процесса.</summary>
public sealed class InMemoryCatalogRepository : ICatalogReader, ICatalogWriter
{
    private readonly SortedDictionary<int, Publication> _publications = new();

    public IReadOnlyList<Publication> GetAll() => Array.AsReadOnly(_publications.Values.ToArray());

    public Publication? FindById(int id) => _publications.GetValueOrDefault(id);

    public void Add(Publication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (!_publications.TryAdd(publication.Id, publication))
        {
            throw new InvalidOperationException($"A publication with ID {publication.Id} already exists.");
        }
    }

    public bool Update(Publication publication)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (!_publications.ContainsKey(publication.Id))
        {
            return false;
        }

        _publications[publication.Id] = publication;
        return true;
    }

    public bool Remove(int id) => _publications.Remove(id);
}
