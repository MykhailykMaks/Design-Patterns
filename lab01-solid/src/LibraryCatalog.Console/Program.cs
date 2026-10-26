using System.Text;
using LibraryCatalog.ConsoleUi;
using LibraryCatalog.Core.Catalog;
using LibraryCatalog.Core.Factories;
using LibraryCatalog.Core.Samples;
using LibraryCatalog.Core.Search;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

// Здесь выбираются конкретные реализации. Сервисы получают их через интерфейсы.
var repository = new InMemoryCatalogRepository();
var catalog = new CatalogService(repository, repository);
var search = new CatalogSearchService(repository);

foreach (var publication in SampleCatalogData.CreateInitial())
{
    catalog.Add(publication);
}

var input = new ConsoleInput(Console.In, Console.Out);
var publicationInput = new PublicationInput(input, Console.Out);
var view = new ConsoleCatalogView(Console.Out);
var factory = new RandomPublicationFactory(new Random());
var menu = new ConsoleMenu(catalog, search, input, publicationInput, view, factory);
menu.Run();
