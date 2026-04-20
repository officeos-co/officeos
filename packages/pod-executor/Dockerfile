FROM golang:1.26-alpine AS builder

WORKDIR /app
COPY go.mod go.sum ./
RUN go mod download
COPY *.go ./
RUN CGO_ENABLED=0 GOOS=linux go build -ldflags="-s -w" -o pod-executor .

FROM alpine:3.20

RUN apk add --no-cache bash curl git python3 && \
    rm -rf /var/cache/apk/*

COPY --from=builder /app/pod-executor /usr/local/bin/pod-executor

EXPOSE 42617

ENTRYPOINT ["pod-executor"]
