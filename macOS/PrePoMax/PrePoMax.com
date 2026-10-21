#!/bin/sh
# PrePoMax for macOS - the console launcher of the batch mode, like PrePoMax.com of Windows: the arguments are
# passed to PrePoMax, which runs in this terminal and ends with the exit code of the run
exec "$(dirname "$0")/PrePoMax" "$@"
