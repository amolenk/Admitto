#!/usr/bin/env bash
set -euo pipefail

echo "################################################################################"
echo "# Installing Oh My OpenCode Slim..."
echo "################################################################################"

bunx oh-my-opencode-slim@latest install --no-tui --skills=yes --background-subagents=yes --companion=no

cp .devcontainer/oh-my-opencode-slim/oh-my-opencode-slim.json \
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

cp .devcontainer/opencode/opencode.json \
   /home/vscode/.config/opencode/opencode.json

cp .devcontainer/opencode/tui.json \
   /home/vscode/.config/opencode/tui.json

echo "################################################################################"
echo "# Configuring Herdr..."
echo "################################################################################"

mkdir -p /home/vscode/.config/herdr
cp .devcontainer/herdr/config.toml \
   /home/vscode/.config/herdr/config.toml

herdr integration install opencode

echo "################################################################################"
echo "# Configuring developer certificate..."
echo "################################################################################"

sudo mkdir -p "$HOME/.aspnet/dev-certs"
sudo chown "$(id -u):$(id -g)" "$HOME/.aspnet" "$HOME/.aspnet/dev-certs"
sudo chmod u+rwx "$HOME/.aspnet" "$HOME/.aspnet/dev-certs"

if [ -z "${SSL_CERT_DIR:-}" ]; then
    export SSL_CERT_DIR="$HOME/.aspnet/dev-certs/trust:/usr/lib/ssl/certs"
else
    export SSL_CERT_DIR="$SSL_CERT_DIR:$HOME/.aspnet/dev-certs/trust"
fi

aspire certs trust

echo "################################################################################"
echo "# Installing Playwright..."
echo "################################################################################"

cd "src/Admitto.UI.Admin" && pnpm install --frozen-lockfile && pnpm exec playwright install --with-deps chromium
