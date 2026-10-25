#!/bin/sh
# Builds NetGenMesher (needs: brew install cmake opencascade gmsh; on Linux GMSH_DIR may name the gmsh SDK)
set -e
cd "$(dirname "$0")"
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release ${GMSH_DIR:+-DGMSH_DIR="$GMSH_DIR"} > /dev/null
cmake --build build -j 8
