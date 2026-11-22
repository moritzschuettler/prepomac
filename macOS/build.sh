#!/bin/bash
# Builds PrePoMax for macOS with a .NET 10 SDK (one in the PATH may be older): ./build.sh [Debug|Release] [dotnet build options]
set -e
cd "$(dirname "$0")"
# cmake lives in Homebrew, and a shell that is not an interactive login shell (ssh, an IDE, MSBuild) has it not in its
# PATH: the Mmg target of the native part would end with exit status 127 in the middle of the build
export PATH="/opt/homebrew/bin:/usr/local/bin:$HOME/.dotnet:$PATH"
for candidate in dotnet /usr/local/share/dotnet/dotnet /opt/homebrew/bin/dotnet "$HOME/.dotnet/dotnet"; do
    if command -v "$candidate" > /dev/null 2>&1 && "$candidate" --list-sdks 2>/dev/null | grep -q "^10\."; then
        DOTNET="$candidate"
        break
    fi
done
if [ -z "$DOTNET" ]; then
    echo "No .NET 10 SDK found. Install it from https://dotnet.microsoft.com/download/dotnet/10.0" >&2
    exit 1
fi
"$DOTNET" build PrePoMax/PrePoMax.csproj -c "${1:-Debug}" "${@:2}"
