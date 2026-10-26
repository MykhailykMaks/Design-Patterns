# Design Patterns — University Coursework

C# coursework for the university subject **Design Patterns / Паттерны проектирования**.

The repository contains two independent projects:

| Folder | Project | Status |
|---|---|---|
| [lab01-solid](lab01-solid/README.md) | Console library catalog demonstrating SOLID | Implemented |
| [game](game/README.md) | WPF game for the following design-pattern labs | Planned |

## Lab 1 — SOLID Library Catalog

A console application for managing books, newspapers and almanacs. It supports manual and factory-generated additions, editing, removal, grouped display and searching by title, publisher, year or author.

- English console interface.
- In-memory storage with fictional demonstration data.
- Separate models, catalog operations, storage, search criteria and console interaction.
- A Simple Factory for fully populated random publications.
- Automated checks for domain rules, CRUD, search, generation and console scenarios.

### Run

Install the .NET 8 SDK, then run from the repository root:

```powershell
cd lab01-solid
.\run.cmd
```

Cross-platform command from the same folder:

```shell
dotnet run --project src/LibraryCatalog.Console -c Release
```

### Check

From `lab01-solid`:

```shell
dotnet build LibraryCatalog.sln -c Release
dotnet run --project tests/LibraryCatalog.Tests -c Release
```

The checks use a standalone executable and require no third-party NuGet packages.

### Study materials

- [Lab instructions](lab01-solid/README.md)
- [Code walkthrough and SOLID examples](lab01-solid/docs/EXPLANATION.md)
- [Defense preparation](lab01-solid/docs/DEFENSE.md)

The study materials are written in Russian. The console interface is English.

## Game — Following Labs

The second project will be developed in `game/`. It will demonstrate suitable creational, structural and behavioral patterns through one evolving WPF game. Its concept and mechanics will be selected after the first lab; no game implementation is included yet.

Each project keeps its own build configuration and instructions so that it can evolve independently.
