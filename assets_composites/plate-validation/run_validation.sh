#!/bin/bash
# Runs the three validation cases with all variants: ./run_validation.sh [work directory inside the home folder]
cd "$(dirname "$0")"
W=${1:-$HOME/PrePoMax/Work/plate-validation}
APP=../../macOS/PrePoMax/bin/Debug/net10.0/PrePoMax.com
for CASE in NAVIER TENSION CANTILEVER; do
    mkdir -p "$W/$CASE"
    PLATE_CASE=$CASE $APP --script "$PWD/plate_validation.py" -g No -x Yes -w "$W/$CASE" --timeout 3500 > "$W/$CASE/out.txt" 2>&1
    grep -a "^$CASE:\|^  [a-z]\|Traceback\|PythonAPIException\|Exception:" "$W/$CASE/out.txt"
done
