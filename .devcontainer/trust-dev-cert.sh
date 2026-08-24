#!/usr/bin/env bash
# Trusts the ASP.NET Core HTTPS dev certificate mounted into the container at
# ASPNETCORE_Kestrel__Certificates__Default__Path.
#
# `dotnet dev-certs https --trust` is not enough here: it only trusts whatever
# cert exists in the container's own dev-certs store, which is unrelated to
# the aspnetapp.pfx bind-mounted from the host. Instead, this extracts the
# actual certificate Kestrel serves and adds it to the system CA trust store,
# which is what .NET's HttpClient (and everything else) validates against.
set -euo pipefail

CERT_PATH="${ASPNETCORE_Kestrel__Certificates__Default__Path:-}"
CERT_PASSWORD="${ASPNETCORE_Kestrel__Certificates__Default__Password:-}"

if [ -z "$CERT_PATH" ] || [ ! -f "$CERT_PATH" ]; then
    echo "trust-dev-cert: no cert at ASPNETCORE_Kestrel__Certificates__Default__Path, skipping."
    exit 0
fi

TMP_CERT="$(mktemp --suffix=.crt)"
trap 'rm -f "$TMP_CERT"' EXIT

if ! openssl pkcs12 -in "$CERT_PATH" -clcerts -nokeys -passin "pass:${CERT_PASSWORD}" -out "$TMP_CERT" 2>/dev/null; then
    openssl pkcs12 -in "$CERT_PATH" -clcerts -nokeys -passin "pass:${CERT_PASSWORD}" -out "$TMP_CERT" -legacy
fi

sudo cp "$TMP_CERT" /usr/local/share/ca-certificates/aspnetcore-dev-cert.crt
sudo update-ca-certificates
