#!/bin/zsh
# Builds the macOS solution (or the given project) and prints the distinct compiler errors
cd "$(dirname "$0")"
target=${1:-PrePoMaxMac.slnx}
root="$(cd .. && pwd)/"
/usr/local/share/dotnet/dotnet build "$target" 2>&1 | grep -E ": error " | sed -E 's# \[[^]]*\]$##' | sed "s#^$root##" | sort -u > /tmp/ppm_errors.txt
echo "$(wc -l < /tmp/ppm_errors.txt) errors"
sed -E 's/^.*error (CS[0-9]+).*$/\1/' /tmp/ppm_errors.txt | sort | uniq -c | sort -rn | head -20
