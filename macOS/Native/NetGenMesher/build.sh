#!/bin/sh
# Builds NetGenMesher (needs: brew install cmake opencascade gmsh)
set -e
cd "$(dirname "$0")"
cmake -S . -B build -DCMAKE_BUILD_TYPE=Release > /dev/null
cmake --build build -j 8
