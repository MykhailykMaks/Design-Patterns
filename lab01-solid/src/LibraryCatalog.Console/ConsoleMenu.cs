using LibraryCatalog.Core.Catalog;
using LibraryCatalog.Core.Models;
using LibraryCatalog.Core.Factories;
using LibraryCatalog.Core.Search;

namespace LibraryCatalog.ConsoleUi;

/// <summary>Связывает команды пользователя с операциями; хранение и поиск делегированы.</summary>
public sealed class ConsoleMenu
{
    private readonly CatalogService _catalog;
    private readonly CatalogSearchService _search;
    private readonly ConsoleInput _input;
    private readonly PublicationInput _publicationInput;
    private readonly ConsoleCatalogView _view;
    private readonly IRandomPublicationFactory _factory;

    public ConsoleMenu(CatalogService catalog, CatalogSearchService search, ConsoleInput input,
        PublicationInput publicationInput, ConsoleCatalogView view, IRandomPublicationFactory factory)
    {
        _catalog = catalog;
        _search = search;
        _input = input;
        _publicationInput = publicationInput;
        _view = view;
        _factory = factory;
    }

    public void Run()
    {
        _view.ShowMessage("LIBRARY CATALOG - LAB 1 / SOLID");
        _view.ShowMessage("Data is kept in memory until the program closes.");
        _view.ShowMessage($"Publications in the catalog: {_catalog.GetAll().Count}");
        try
        {
            RunCommands();
        }
        catch (EndOfStreamException)
        {
            _view.ShowMessage("\nInput ended. Unfinished changes were not saved.");
        }
    }

    private void RunCommands()
    {
        while (true)
        {
            _view.ShowMenu();
            string command = _input.ReadLine("Command: ");
            try
            {
                switch (command)
                {
                    case "1":
                        _view.ShowAll(_catalog.GetAll());
                        break;
                    case "2":
                        _view.ShowAll(_catalog.GetAll(), grouped: true);
                        break;
                    case "3":
                        AddChosen();
                        break;
                    case "4":
                        AddRandom();
                        break;
                    case "5":
                        Edit();
                        break;
                    case "6":
                        Remove();
                        break;
                    case "7":
                        Search();
                        break;
                    case "0":
                        _view.ShowMessage("Session finished. Goodbye!");
                        return;
                    default:
                        _view.ShowMessage("Unknown command. Choose a menu option.");
                        break;
                }
            }
            catch (ArgumentException exception)
            {
                _view.ShowMessage($"Invalid data: {exception.Message}");
            }
            catch (InvalidOperationException exception)
            {
                _view.ShowMessage($"Operation failed: {exception.Message}");
            }
            catch (OverflowException)
            {
                _view.ShowMessage("No more publication IDs are available. Nothing was added.");
            }
        }
    }

    private void AddChosen()
    {
        _view.ShowMessage("Type: 1 - Book, 2 - Newspaper, 3 - Almanac.");
        int type = _input.ReadInt("Publication type", 1, 3);
        Publication publication = _publicationInput.Create(_catalog.NextId(), type);
        _catalog.Add(publication);
        _view.ShowMessage($"Added publication with ID {publication.Id}.");
    }

    private void AddRandom()
    {
        Publication publication = _factory.Create(_catalog.NextId());
        _catalog.Add(publication);
        _view.ShowMessage($"Added publication with ID {publication.Id}.");
        _view.ShowPublication(publication);
    }

    private void Edit()
    {
        int id = _input.ReadInt("Publication ID to edit");
        Publication? publication = _catalog.FindById(id);
        if (publication is null)
        {
            _view.ShowMessage($"Publication with ID {id} not found.");
            return;
        }

        _view.ShowPublication(publication);
        Publication replacement = _publicationInput.Edit(publication);
        bool updated = _catalog.Update(replacement);
        _view.ShowMessage(updated ? $"Publication with ID {id} updated." : $"Publication with ID {id} not found.");
    }

    private void Remove()
    {
        int id = _input.ReadInt("Publication ID to remove");
        bool removed = _catalog.Remove(id);
        _view.ShowMessage(removed ? $"Publication with ID {id} removed." : $"Publication with ID {id} not found.");
    }

    private void Search()
    {
        _view.ShowMessage("Search: 1 - Title, 2 - Publisher, 3 - Year, 4 - Author.");
        int field = _input.ReadInt("Search field", 1, 4);
        ISearchCriterion criterion = field switch
        {
            1 => new TitleCriterion(_input.ReadText("Title or part of a title")),
            2 => new PublisherCriterion(_input.ReadText("Publisher or part of a name")),
            3 => new YearCriterion(_input.ReadInt("Publication year", 1, 9999)),
            _ => new AuthorCriterion(_input.ReadText("Author or part of a name"))
        };
        _view.ShowSearchResults(_search.Search(criterion));
    }
}
