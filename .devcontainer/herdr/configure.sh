#!/usr/bin/env bash
set -euo pipefail

workspace_folder="${1:-$PWD}"

cp "$workspace_folder/.devcontainer/herdr/config.toml" \
   "/home/vscode/.config/herdr/config.toml"