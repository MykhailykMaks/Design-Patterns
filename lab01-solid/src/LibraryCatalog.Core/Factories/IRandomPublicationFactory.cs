using LibraryCatalog.Core.Models;

namespace LibraryCatalog.Core.Factories;

/// <summary>Создаёт готовое издание случайного типа с переданным идентификатором.</summary>
public interface IRandomPublicationFactory
{
    Publication Create(int id);
}
