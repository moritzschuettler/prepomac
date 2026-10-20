#!/bin/sh
# Builds the Windows API compatibility library and installs it under the names .NET probes for kernel32.dll and user32.dll
set -e
DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${1:-$DIR/bin}"
mkdir -p "$OUT"
clang -dynamiclib -O2 -arch arm64 -arch x86_64 -o "$OUT/libWin32Compat.dylib" "$DIR/Win32Compat.c" -install_name @rpath/libWin32Compat.dylib
for name in libkernel32.dll.dylib libuser32.dll.dylib libuser32.dylib; do cp "$OUT/libWin32Compat.dylib" "$OUT/$name"; done
