#!/bin/sh
# Builds the Windows API compatibility library and installs it under the names .NET probes for kernel32.dll and user32.dll
# (.dylib files on macOS, .so files on Linux)
set -e
DIR="$(cd "$(dirname "$0")" && pwd)"
OUT="${1:-$DIR/bin}"
mkdir -p "$OUT"
if [ "$(uname -s)" = "Linux" ]; then
    cc -shared -fPIC -O2 -o "$OUT/libWin32Compat.so" "$DIR/Win32Compat.c" -ldl
    for name in libkernel32.dll.so libuser32.dll.so libuser32.so; do cp "$OUT/libWin32Compat.so" "$OUT/$name"; done
else
    clang -dynamiclib -O2 -arch arm64 -arch x86_64 -o "$OUT/libWin32Compat.dylib" "$DIR/Win32Compat.c" -install_name @rpath/libWin32Compat.dylib
    for name in libkernel32.dll.dylib libuser32.dll.dylib libuser32.dylib; do cp "$OUT/libWin32Compat.dylib" "$OUT/$name"; done
fi
