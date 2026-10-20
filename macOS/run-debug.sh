#!/bin/sh
# Starts the macOS build of PrePoMax for a number of seconds and writes the output and the exceptions to /tmp/ppm_run.log
SECONDS_TO_RUN="${1:-35}"
DIR="$(cd "$(dirname "$0")" && pwd)/PrePoMax/bin/Debug/net10.0"
cd "$DIR" || exit 1
PPM_TRACE=1 /usr/local/share/dotnet/dotnet PrePoMax.dll > /tmp/ppm_run.log 2>&1 &
PID=$!
sleep "$SECONDS_TO_RUN"
if kill -0 $PID 2>/dev/null; then echo "RUNNING after $SECONDS_TO_RUN s"; kill $PID; else echo "EXITED"; fi
