#!/bin/bash
# PrePoMax for macOS - checks that a PrePoMax.app carries everything it needs:
#
#   ./verify-bundle.sh <PrePoMax.app>                 an app that uses the Homebrew libraries of this Mac
#   ./verify-bundle.sh <PrePoMax.app> --standalone    an app that must run without Homebrew
#
# make-app.sh runs this on the staged bundle before it installs anything. A bundle that misses Solver, Python or
# Frameworks does not fail at the build but at the first solve, hours later; this check turns that into an error at the
# end of the build.
#
# Without --standalone the app loads libgmsh.dylib and liblpsolve55.dylib from Homebrew and starts CalculiX from
# /opt/homebrew/bin, /usr/local/bin or the PATH (PrePoMax falls back to those), so these are checked here on the Mac
# instead of inside the app. PPM_BREW_PREFIX names a Homebrew that is not in /opt/homebrew (Intel Macs: /usr/local).
#
# Exit status: 0 complete, 1 incomplete (the missing parts are named), 2 wrong use.

APP=""
STANDALONE=0
for argument in "$@"; do
    case "$argument" in
        --standalone) STANDALONE=1 ;;
        -*) echo "unknown option: $argument" >&2; exit 2 ;;
        *) APP="$argument" ;;
    esac
done
if [ -z "$APP" ]; then
    echo "usage: $0 <PrePoMax.app> [--standalone]" >&2
    exit 2
fi
if [ ! -d "$APP" ]; then
    echo "not a folder: $APP" >&2
    exit 1
fi
APP="$(cd "$APP" && pwd)"
MACOS="$APP/Contents/MacOS"
BREW_PREFIX="${PPM_BREW_PREFIX:-}"
if [ -z "$BREW_PREFIX" ]; then
    if [ -d /opt/homebrew ]; then BREW_PREFIX=/opt/homebrew; else BREW_PREFIX=/usr/local; fi
fi

MISSING=()
ok()   { printf '  ok       %s\n' "$1"; }
miss() { printf '  MISSING  %s\n' "$1"; MISSING+=("$1: $2"); }
# A file or program the app cannot work without; what is given is the sentence that says why
need() {
    if [ -e "$1" ]; then ok "${1#$APP/}"; else miss "${1#$APP/}" "$2"; fi
}
need_exec() {
    if [ -x "$1" ]; then ok "${1#$APP/}"; else miss "${1#$APP/}" "$2"; fi
}
# Where CalculiX is, the way PrePoMax looks for it at start (StartJob/Settings/SolverSettings.cs FindCalculixExecutable)
find_ccx() {
    for directory in "$BREW_PREFIX/bin" /usr/local/bin; do
        if [ -f "$directory/ccx" ]; then echo "$directory/ccx"; return; fi
        newest=$(ls -1 "$directory"/ccx_* 2>/dev/null | tail -1)
        if [ -n "$newest" ]; then echo "$newest"; return; fi
    done
    if command -v ccx > /dev/null 2>&1; then command -v ccx; return; fi
    echo ""
}

echo "Checking $APP"
if [ "$STANDALONE" = 1 ]; then echo "  (standalone: the app must carry the libraries, CalculiX and a Python)"; fi
echo
echo "The app:"
need "$MACOS/PrePoMax" "the program itself is missing; the .NET build did not produce it"
need "$MACOS/PrePoMax.com" "the command line launcher is missing (it starts PrePoMax.com of the batch mode)"
need_exec "$MACOS/PrePoMaxLauncher" "the launcher the bundle starts (CFBundleExecutable) is missing"
need "$APP/Contents/Info.plist" "without it the folder is not an app for Finder and the Launchpad"
need_exec "$MACOS/Meshers/Mmg/mmg3d" "the Mmg remesher is missing, so remeshing fails"
need_exec "$MACOS/Meshers/Mmg/mmgs" "the Mmg remesher is missing, so remeshing fails"
need_exec "$MACOS/Meshers/Netgen/NetGenMesher" "the CAD import and the Netgen mesh items run in this program"
need "$APP/Contents/Resources/Licenses" "the license texts of the components are missing"

if [ "$STANDALONE" = 1 ]; then
    echo
    echo "Inside the app:"
    need "$MACOS/Solver/ccx" "CalculiX is missing, so no analysis can run (bundle-deps.py found no ccx: brew install costerwi/calculix/calculix-ccx, or ./make-app.sh --ccx <path>)"
    need_exec "$MACOS/Python/bin/python3" "the app's own Python is missing, so the Python scripts do not run"
    need "$APP/Contents/Frameworks/liblpsolve55.dylib" "lp_solve is missing, so the quad meshing (interval assignment) fails at the first mesh"
    need "$APP/Contents/Frameworks/libgmsh.dylib" "gmsh is missing, so no meshing works in this app"
    if [ -d "$APP/Contents/Frameworks" ]; then
        count=$(ls -1 "$APP/Contents/Frameworks" | wc -l | tr -d ' ')
        if [ "$count" -ge 2 ]; then ok "Contents/Frameworks ($(du -sm "$APP/Contents/Frameworks" | cut -f1) MB, $count files)"; fi
    fi
else
    echo
    echo "On this Mac, which the app uses for gmsh, lp_solve and CalculiX (Homebrew: $BREW_PREFIX):"
    if [ -f "$BREW_PREFIX/lib/libgmsh.dylib" ] || [ -f /usr/local/lib/libgmsh.dylib ]; then
        ok "libgmsh.dylib"
    else
        miss "libgmsh.dylib" "gmsh is not installed: brew install gmsh (or build with --standalone)"
    fi
    if [ -f "$BREW_PREFIX/lib/liblpsolve55.dylib" ] || [ -f /usr/local/lib/liblpsolve55.dylib ]; then
        ok "liblpsolve55.dylib"
    else
        miss "liblpsolve55.dylib" "lp_solve is not installed: brew install lp_solve (or build with --standalone)"
    fi
    ccx=$(find_ccx)
    if [ -n "$ccx" ]; then ok "CalculiX: $ccx"; else miss "CalculiX" "no ccx found: brew install costerwi/calculix/calculix-ccx (or build with --standalone)"; fi
fi

echo
if [ ${#MISSING[@]} -gt 0 ]; then
    echo "Incomplete: this app would fail at meshing or solving, not at the start."
    for line in "${MISSING[@]}"; do echo "  - $line"; done
    if [ "$STANDALONE" = 0 ]; then
        echo
        echo "For an app that needs nothing installed on the Mac, build it with ./make-app.sh --standalone"
    fi
    exit 1
fi
echo "Complete ($(du -sm "$APP" | cut -f1) MB)."
exit 0