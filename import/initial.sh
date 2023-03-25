#!/usr/bin/env bash
# This script is used to import the initial data into the database.

set -eo pipefail

BASEDIR=$(dirname "$0")

echo "=============================================="
if [ -f ./data/extract.osm.pbf ]; then
    echo "Extract already exists, skipping download"
else
    echo "Downloading extract from ${EXTRACT_URL}"
    wget -O ./data/extract.osm.pbf "${EXTRACT_URL}"
fi

echo "=============================================="
echo "Cropping extract to bbox: ${BBOX}"
osmium extract --overwrite -b "${BBOX}" -F pbf -s simple -f pbf -o ./data/extract_cropped.osm.pbf ./data/extract.osm.pbf

echo "=============================================="
echo "Importing extract"
osm2pgsql --create --slim --output flex --style "${BASEDIR}/config.lua" -d "${PGDATABASE}" ./data/extract_cropped.osm.pbf

echo "=============================================="
echo "Initializing updates from ${UPDATE_SERVER_URL}"
osm2pgsql-replication init --server "$UPDATE_SERVER_URL" -d "${PGDATABASE}"

