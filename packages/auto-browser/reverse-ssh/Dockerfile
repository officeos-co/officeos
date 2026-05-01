FROM alpine:3.21

RUN apk add --no-cache autossh openssh-client

COPY entrypoint.sh /entrypoint.sh

RUN chmod +x /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]
