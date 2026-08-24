#!/usr/bin/env bash
set -euo pipefail

workspace_folder="${1:-$PWD}"

cp "$workspace_folder/.devcontainer/opencode/opencode.json" \
   "/home/vscode/.config/opencode/opencode.json"