#!/bin/sh
# Builds libgmsh.dylib from the patched Gmsh 4.13 that PrePoMax ships on Windows (github.com/PrePoMax/Gmsh, branch
# prepomax-quadqs-transfinite: the quasi-structured quad meshing leaves the counts of transfinite curves alone, never
# folds a quadrangle into triangles, and sizes a curve by the curvature of its faces along it) into build/, against the
# OpenCASCADE of Homebrew. Needs git, cmake and brew install opencascade.
set -e
cd "$(dirname "$0")"
COMMIT=4156db5d5647087b1575c3f4b8a65a891b81102b
if [ ! -d src ]; then
    git clone --depth 1 --branch prepomax-quadqs-transfinite https://github.com/PrePoMax/Gmsh.git src
fi
if [ "$(git -C src rev-parse HEAD)" != "$COMMIT" ]; then
    git -C src fetch --depth 1 origin "$COMMIT"
    git -C src checkout --quiet "$COMMIT"
fi
GENERATOR=""
command -v ninja > /dev/null 2>&1 && GENERATOR="-G Ninja"
PREFIX=$(brew --prefix 2>/dev/null || echo /opt/homebrew)
cmake -S src -B build $GENERATOR -DCMAKE_BUILD_TYPE=Release -DENABLE_BUILD_DYNAMIC=ON -DENABLE_FLTK=OFF -DENABLE_OCC=ON \
      -DENABLE_OCC_CAF=OFF -DENABLE_PETSC=OFF -DENABLE_SLEPC=OFF -DENABLE_MPI=OFF -DENABLE_OPENMP=OFF -DENABLE_CGNS=OFF \
      -DENABLE_MED=OFF -DENABLE_OS_SPECIFIC_INSTALL=OFF -DCMAKE_PREFIX_PATH="$PREFIX" > /dev/null
cmake --build build -j 8 --target shared
