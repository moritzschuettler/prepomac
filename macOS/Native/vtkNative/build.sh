#!/bin/sh
# Builds libvtkNative.dylib, the 3D view, from vtkNative/ of the repository against the VTK of Homebrew
# (needs: brew install cmake vtk; VTK_DIR may name another VTK 9.7 build)
set -e
cd "$(dirname "$0")/../../.."
VTK_DIR=${VTK_DIR:-$(brew --prefix vtk 2>/dev/null)/lib/cmake/vtk-9.7}
GENERATOR=""
command -v ninja > /dev/null 2>&1 && GENERATOR="-G Ninja"
cmake -S vtkNative -B vtkNative/build-mac $GENERATOR -DCMAKE_BUILD_TYPE=Release -DVTK_DIR="$VTK_DIR" > /dev/null
cmake --build vtkNative/build-mac -j 8
