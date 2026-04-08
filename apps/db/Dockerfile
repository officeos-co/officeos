# syntax=docker/dockerfile:1

FROM postgres:16-alpine

# Self-contained: bake credentials into the image via ENV.
# postgres:16-alpine's entrypoint reads POSTGRES_* at container start.
ENV POSTGRES_DB=enterprise_agent_os \
    POSTGRES_USER=eaos \
    POSTGRES_PASSWORD=eaos

# Keep the .env alongside for parity with the other services.
COPY .env /etc/eaos/db.env
