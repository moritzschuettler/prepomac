#!/bin/sh
# Builds the Mmg remeshers mmgs and mmg3d (version 5.7.0, the version of the Windows build) from the upstream
# sources into build/bin. Needs git and cmake.
set -e
cd "$(dirname "$0")"
# The build runs inside MSBuild, which does not inherit Homebrew's bin from an interactive shell; without it cmake is
# "not found" and the build ends with exit status 127
export PATH="/opt/homebrew/bin:/usr/local/bin:$PATH"
if ! command -v cmake > /dev/null 2>&1; then
    echo "cmake not found: brew install cmake" >&2
    exit 1
fi
if [ ! -d src ]; then
    git clone --depth 1 --branch v5.7.0 https://github.com/MmgTools/mmg.git src
fi
cmake -S src -B build -DCMAKE_BUILD_TYPE=Release -DBUILD_TESTING=OFF -DUSE_SCOTCH=OFF -DUSE_ELAS=OFF -DUSE_VTK=OFF \
      -DLIBMMG3D_STATIC=ON -DLIBMMGS_STATIC=ON -DLIBMMG2D_STATIC=ON -DCMAKE_POLICY_VERSION_MINIMUM=3.5 > /dev/null
cmake --build build -j 8 --target mmgs mmg3d
