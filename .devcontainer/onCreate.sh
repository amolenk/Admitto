#!/usr/bin/env bash
set -euo pipefail

echo "################################################################################"
echo "# Installing Oh My OpenCode Slim..."
echo "################################################################################"

bunx oh-my-opencode-slim@latest install --no-tui --skills=yes --background-subagents=yes --companion=no

cp /workspaces/Admitto/.devcontainer/oh-my-opencode-slim/oh-my-opencode-slim.json \
   /home/vscode/.config/opencode/oh-my-opencode-slim.json

echo "################################################################################"
echo "# Configuring OpenCode..."
echo "################################################################################"

cp /workspaces/Admitto/.devcontainer/opencode/opencode.json \
   /home/vscode/.config/opencode/opencode.json

echo "################################################################################"
echo "# Configuring Herdr..."
echo "################################################################################"

mkdir -p /home/vscode/.config/herdr
cp /workspaces/Admitto/.devcontainer/herdr/config.toml \
   /home/vscode/.config/herdr/config.toml

echo "################################################################################"
echo "# Trusting .NET development certificate..."
echo "################################################################################"

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
