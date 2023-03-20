FROM debian:bookworm-slim

RUN apt -y update  \
    && apt -y install osmium-tool osm2pgsql wget  \
    && apt -y clean

WORKDIR /app
RUN mkdir -p ./data

COPY ./import ./import

RUN chmod +x ./import/*.sh

ENTRYPOINT ["/app/import/import.sh"]

