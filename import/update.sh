#!/usr/bin/env bash
# This script is used to run database updates

BASEDIR=$(dirname "$0")

echo "=============================================="
echo "Running updates from ${UPDATE_SERVER_URL}"
osm2pgsql-replication update -d "${PGDATABASE}" -- -b "${BBOX}" --output flex --style "${BASEDIR}/config.lua"
