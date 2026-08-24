#!/usr/bin/env bash
set -euo pipefail

echo "################################################################################"
echo "# Installing Oh My OpenCode Slim..."
echo "################################################################################"

bunx oh-my-opencode-slim@latest install --no-tui --skills=yes --background-subagents=yes --companion=no

cp /workspaces/Admitto/.devcontainer/oh-my-opencode-slim/oh-my-opencode-slim.json \
   /home/vscode/.config/opencode/oh-my-opencode-slim.json

if ! grep -Fq 'omos() {' /home/vscode/.zshrc; then
      cat <<'EOF' >> /home/vscode/.zshrc

omos() {
   local port arg

   for arg in "$@"; do
      if [[ "$arg" == --port=* ]]; then
         port="${arg#--port=}"
         break
      fi
   done

   if [[ -z "$port" ]]; then
      local -a args=("$@")
      local -i index
      for ((index = 1; index <= ${#args}; index++)); do
         if [[ "${args[index]}" == --port ]]; then
            port="${args[index + 1]}"
            break
         fi
      done
   fi

   if [[ -n "$port" ]]; then
      OPENCODE_PORT="$port" command opencode "$@"
      return
   fi

   port=$(python3 -c 'import socket; s = socket.socket(); s.bind(("127.0.0.1", 0)); print(s.getsockname()[1]); s.close()') || return
   OPENCODE_PORT="$port" command opencode --port "$port" "$@"
}
EOF
fi

echo "################################################################################"
echo "# Configuring OpenCode..."
echo "################################################################################"

cp /workspaces/Admitto/.devcontainer/opencode/opencode.json \
   /home/vscode/.config/opencode/opencode.json

cp /workspaces/Admitto/.devcontainer/opencode/tui.json \
   /home/vscode/.config/opencode/tui.json

echo "################################################################################"
echo "# Configuring Herdr..."
echo "################################################################################"

mkdir -p /home/vscode/.config/herdr
cp /workspaces/Admitto/.devcontainer/herdr/config.toml \
   /home/vscode/.config/herdr/config.toml

herdr integration install opencode

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
