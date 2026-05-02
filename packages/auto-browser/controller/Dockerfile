FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

RUN set -eux; \
    apt-get update -o Acquire::Retries=5; \
    DEBIAN_FRONTEND=noninteractive apt-get install -y --no-install-recommends \
        autossh \
        openssh-client \
        tesseract-ocr \
        tesseract-ocr-eng; \
    rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app

RUN mkdir -p /data/artifacts /data/uploads /data/auth /data/approvals /data/audit /data/db /data/jobs /data/sessions /data/browser-sessions /data/tunnels /data/cli-home

EXPOSE 8000

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
