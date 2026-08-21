FROM mcr.microsoft.com/devcontainers/dotnet:dev-10.0-noble

ARG NODE_VERSION=24.19.0
ARG CLAUDE_CODE_VERSION=latest

USER root

# Prepare directories that are not runtime-mounted volumes.
#RUN mkdir -p /home/vscode/.claude \
#    && chown -R vscode:vscode /home/vscode/.claude

# Install Node and Claude as the non-root runtime user.
USER vscode

#ENV PATH="/home/vscode/.local/bin:${PATH}"

#RUN curl -fsSL https://claude.ai/install.sh | bash

#SHELL ["/bin/bash", "-c"]

#RUN source "${NVM_DIR}/nvm.sh" \
 #   && nvm install "${NODE_VERSION}" \
  #  && nvm alias default "${NODE_VERSION}" \
   # && nvm use default \
    #&& npm install --global "@anthropic-ai/claude-code@${CLAUDE_CODE_VERSION}"

#USER vscode