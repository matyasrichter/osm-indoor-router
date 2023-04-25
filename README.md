# OpenStreetMap Indoor Router

This repository contains the sources for an indoor navigation solution based on [OpenStreetMap](https://www.openstreetmap.org/about) data.

## [Live Demo](https://nav.mrichter.dev/)

## Development
- Install the .NET SDK from https://dotnet.microsoft.com/en-us/download
- To automatically run formatters before commiting, install [pre-commit](https://pre-commit.com/) and register the required hooks:
```shell
# register a git pre-commit hook
pre-commit install

# run hooks without commiting
pre-commit run

# run hooks on all (staged+unstaged) files
pre-commit run --all-files
```

## Running tests
```shell
dotnet test
```

Some tests require a database. We're using [Testcontainers](https://www.testcontainers.org/) to create the DB. This creates a dependency on Docker.
To only run non-testcontainers tests, use `dotnet test --filter "Category!=DB"`.


