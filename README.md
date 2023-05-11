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


## Running locally (production-like)
Prerequisities: Docker and Docker Compose

Create copies of the example environment configs:
```
cp .env.db.example .env.db
cp .env.import.example .env.import
cp ./frontend/.env.example ./frontend/.env
cp appsettings.example.json appsettings.Production.json
```

Generate your MapTiler and IndoorEqual API keys and add them to `./frontend/.env`:
MapTiler link: https://cloud.maptiler.com/maps/
IndoorEqual link: https://indoorequal.com/users/register

Start the database:
```
docker compose -f docker-compose.local.yml up -d db
```

Start the import:
```
docker compose -f docker-compose.local.yml up import-init
```

Start the seconds phase of import, graph building
```
docker compose -f docker-compose.local.yml up updater
```

Start the API and the frontend:
```
docker compose -f docker-compose.local.yml up -d frontend api
```

Before production deployment:
- Set up CORS (the local setup uses a wildcard)
- Set up map update scheduling (see `docker-compose.production.yml`)

