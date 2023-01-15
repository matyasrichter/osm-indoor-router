# OpenStreetMap Indoor Router

This repository contains the sources for an indoor navigation solution based on [OpenStreetMap](https://www.openstreetmap.org/about) data.

## Development
- Install the .NET SDK from https://dotnet.microsoft.com/en-us/download
- To automatically run formatters before commiting, use the provided pre-commit script:
```shell
ln -s "$PWD/pre-commit.sh" "`git rev-parse --git-dir`/hooks/pre-commit"
```
