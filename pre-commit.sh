#!/usr/bin/env bash

set -eo pipefail

echo "Running: dotnet-format"
dotnet format --verify-no-changes

echo "SUCCESS!"
