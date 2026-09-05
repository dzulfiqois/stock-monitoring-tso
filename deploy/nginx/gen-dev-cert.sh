#!/bin/sh
# Generate sertifikat self-signed untuk pengujian TLS lokal (bukan untuk produksi).
# Output: deploy/nginx/certs/fullchain.pem + privkey.pem
set -e
DIR="$(cd "$(dirname "$0")" && pwd)/certs"
mkdir -p "$DIR"
openssl req -x509 -nodes -newkey rsa:2048 -days 365 \
  -keyout "$DIR/privkey.pem" \
  -out "$DIR/fullchain.pem" \
  -subj "/CN=stockmonitor.local" \
  -addext "subjectAltName=DNS:localhost,DNS:stockmonitor.local"
echo "Sertifikat dibuat di $DIR"
echo "Mount ke nginx: tambahkan volume berikut di docker-compose.yml:"
echo '  - ./deploy/nginx/certs:/etc/nginx/certs:ro'
