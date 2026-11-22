#!/bin/sh
# Builds NetGenMesher (needs: brew install cmake opencascade gmsh; on Linux GMSH_DIR may name the gmsh SDK)
set -e
cd "$(dirname "$0")"
# MSBuild starts this without the PATH of an interactive shell, which is where Homebrew's cmake is
export PATH="/opt/homebrew/bin:/usr/local/bin:$PATH"
if ! command -v cmake > /dev/null 2>&1; then
    echo "cmake not found: brew install cmake" >&2
    exit 1
fi
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release ${GMSH_DIR:+-DGMSH_DIR="$GMSH_DIR"} > /dev/null
cmake --build build -j 8
