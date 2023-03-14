FROM postgis/postgis:15-3.3

RUN apt -y update && apt -y install postgresql-15-pgrouting

