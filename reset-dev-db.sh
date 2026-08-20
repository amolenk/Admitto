#!/usr/bin/env bash

set -Eeuo pipefail

readonly POSTGRES_RESOURCE="postgres"
readonly POSTGRES_VOLUME="admitto-postgres"

usage() {
    printf 'Usage: %s\n\n' "${0##*/}"
    printf 'Stop the local Aspire %s resource and remove its persisted PostgreSQL data.\n' "$POSTGRES_RESOURCE"
    printf 'The next "aspire start" will create a clean database.\n'
}

if [[ "${1:-}" == "-h" || "${1:-}" == "--help" ]]; then
    usage
    exit 0
fi

if (( $# > 0 )); then
    printf 'Error: unexpected argument: %s\n\n' "$1" >&2
    usage >&2
    exit 2
fi

if ! command -v docker >/dev/null 2>&1; then
    printf 'Error: Docker is unavailable. Install Docker and ensure it is on PATH.\n' >&2
    exit 127
fi

if ! docker info >/dev/null 2>&1; then
    printf 'Error: Docker is unavailable or its daemon is not running. Start Docker and try again.\n' >&2
    exit 1
fi

printf 'Resetting Aspire resource "%s" (volume "%s")...\n' "$POSTGRES_RESOURCE" "$POSTGRES_VOLUME"

# Aspire may generate a dynamic container name. Selecting containers by the
# explicitly configured data volume avoids stopping unrelated containers.
running_containers="$(docker ps -q --filter "volume=${POSTGRES_VOLUME}")"
if [[ -n "$running_containers" ]]; then
    while IFS= read -r container_id; do
        [[ -z "$container_id" ]] || docker stop "$container_id" >/dev/null
    done <<< "$running_containers"
fi

all_containers="$(docker ps -aq --filter "volume=${POSTGRES_VOLUME}")"
if [[ -n "$all_containers" ]]; then
    while IFS= read -r container_id; do
        [[ -z "$container_id" ]] || docker rm "$container_id" >/dev/null
    done <<< "$all_containers"
fi

if docker volume inspect "$POSTGRES_VOLUME" >/dev/null 2>&1; then
    docker volume rm "$POSTGRES_VOLUME" >/dev/null
    printf 'Removed PostgreSQL volume "%s".\n' "$POSTGRES_VOLUME"
else
    printf 'PostgreSQL volume "%s" does not exist; nothing to remove.\n' "$POSTGRES_VOLUME"
fi

printf 'Development database reset complete.\n'
