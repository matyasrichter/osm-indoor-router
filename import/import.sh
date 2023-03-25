#!/usr/bin/env bash
# This script is contains checks for env variables, is used as the entrypoint for the import container

set -eo pipefail

if [ -z "${EXTRACT_URL}" ]; then
    echo "EXTRACT_URL is not set"
    echo "Please set the EXTRACT_URL environment variable to the URL of the extract to import"
    echo "Example: export EXTRACT_URL=https://download.geofabrik.de/europe/czech-republic-latest.osm.pbf"
    exit 1
fi

if [ -z "${UPDATE_SERVER_URL}" ]; then
    echo "UPDATE_SERVER_URL is not set"
    echo "Please set the UPDATE_SERVER_URL environment variable to a URL where changefiles can be fetched from"
    echo "Example: export UPDATE_SERVER_URL=https://download.geofabrik.de/europe/czech-republic-updates"
    exit 1
fi

if [ -z "${BBOX}" ]; then
    echo "BBOX is not set"
    echo "Please set the BBOX environment variable to the bounding box of the area to import"
    echo "Remember to use a slightly larger bounding box than the area you want to import, to avoid missing data"
    echo "Example: export BBOX=14.38534,50.10208,14.39604,50.10648"
    exit 1
fi

if [ -z "${PGDATABASE}" ]; then
    echo "PGDATABASE is not set"
    echo "Please set the PGDATABASE environment variable"
    echo "Example: export PGDATABASE=indoorrouter"
    exit 1
fi

exec "$@"
