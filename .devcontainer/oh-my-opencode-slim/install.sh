#!/usr/bin/env bash
set -euo pipefail

workspace_folder="${1:-$PWD}"

bunx oh-my-opencode-slim@latest install --no-tui --skills=yes --background-subagents=yes

cp "$workspace_folder/.devcontainer/oh-my-opencode-slim/oh-my-opencode-slim.json" \
   "/home/vscode/.config/opencode/oh-my-opencode-slim.json"