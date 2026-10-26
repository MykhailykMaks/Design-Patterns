using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Catalog;

/// <summary>Операции каталога; конкретное хранилище передаётся снаружи.</summary>
public sealed class CatalogService
{
    private readonly ICatalogReader _reader;
    private readonly ICatalogWriter _writer;
    private int _lastIssuedId;

    public CatalogService(ICatalogReader reader, ICatalogWriter writer)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(writer);
        _reader = reader;
        _writer = writer;
    }

    public IReadOnlyList<Publication> GetAll() => _reader.GetAll();
    public Publication? FindById(int id) => _reader.FindById(id);

    public int NextId()
    {
        int currentMaximum = _reader.GetAll().Select(item => item.Id).DefaultIfEmpty(0).Max();
        _lastIssuedId = checked(Math.Max(_lastIssuedId, currentMaximum) + 1);
        return _lastIssuedId;
    }

    public void Add(Publication publication) => _writer.Add(publication);

    public bool Update(Publication replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        Publication? existing = _reader.FindById(replacement.Id);
        if (existing is null)
        {
            return false;
        }

        if (existing.GetType() != replacement.GetType())
        {
            throw new InvalidOperationException("The publication type cannot be changed while editing.");
        }

        return _writer.Update(replacement);
    }

    public bool Remove(int id) => _writer.Remove(id);
}
