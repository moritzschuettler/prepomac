#!/bin/bash
# Starts PrePoMax for macOS after ./build.sh: ./run.sh [Debug|Release]
cd "$(dirname "$0")/PrePoMax/bin/${1:-Debug}/net10.0" || { echo "Build first: ./build.sh" >&2; exit 1; }
exec ./PrePoMax
