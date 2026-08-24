FROM mcr.microsoft.com/devcontainers/dotnet:dev-10.0-noble

ARG NODE_VERSION=24.19.0
ARG CLAUDE_CODE_VERSION=latest

USER root

# Ensure named-volume mount points retain the runtime user's ownership.
RUN mkdir -p /home/vscode/.claude \
        /home/vscode/.config/opencode \
        /home/vscode/.local/share/opencode \
        /home/vscode/.config/herdr \
        /home/vscode/.local/state/herdr \
    && chown -R vscode:vscode /home/vscode/.claude /home/vscode/.config /home/vscode/.local

# Install coding agents as the non-root runtime user.
USER vscode

ENV PATH="/home/vscode/.local/bin:/home/vscode/.opencode/bin:${PATH}"

RUN curl -fsSL https://claude.ai/install.sh | bash
RUN curl -fsSL https://opencode.ai/install | bash
RUN curl -fsSL https://herdr.dev/install.sh | sh
