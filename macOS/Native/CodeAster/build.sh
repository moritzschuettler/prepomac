#!/bin/bash
# Builds the Code_Aster image PrePoMax runs in Docker (needs Docker, e.g. colima with Rosetta on Apple Silicon:
# colima start aster --vm-type vz --vz-rosetta)
set -e
cd "$(dirname "$0")"
docker ${DOCKER_CONTEXT:+--context "$DOCKER_CONTEXT"} build --platform linux/amd64 -t prepomax/code_aster:18.1 .
