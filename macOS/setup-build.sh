#!/bin/bash
# PrePoMax for macOS - checks the tools a build needs and then builds and installs PrePoMax.app.
#
#   ./setup-build.sh                      check the tools, then build and install the standalone app
#   ./setup-build.sh --check              only check, build nothing
#   ./setup-build.sh --install-tools      install the missing Homebrew packages first
#   ./setup-build.sh --ccx <path>         use this CalculiX, wherever it lies (else the one of Homebrew)
#   ./setup-build.sh <folder>             install into <folder> instead of /Applications or ~/Applications
#
# Why this script exists: a shell that is not an interactive login shell (ssh, cron, an IDE, a CI runner, .NET's
# MSBuild) has no /opt/homebrew/bin in its PATH. Then cmake is "not found", the Mmg target of the build ends with exit
# status 127, and the build stops in the middle without saying why. Everything below therefore sets the PATH itself
# instead of trusting the caller's shell, checks every tool before the build starts, and names all missing ones in one
# message that carries the command that installs them.
#
# The tools are checked where the build and bundle-deps.py use them: cmake as a program, libgmsh.dylib and
# liblpsolve55.dylib as files in the Homebrew prefix (bundle-deps.py copies exactly those into the app), ccx as the
# CalculiX the app starts, python3 because bundle-deps.py needs 3.12 or newer (macOS's /usr/bin/python3 is 3.9; it
# cannot unpack the Python that goes into the app).
#
# PPM_BREW_PREFIX=/some/prefix checks against another Homebrew (Intel Macs: /usr/local).

cd "$(dirname "$0")" || exit 1
HERE="$(pwd)"

MODE=build
INSTALL_TOOLS=0
GIVEN_CCX=""
INSTALL_FOLDER=""
while [ $# -gt 0 ]; do
    case "$1" in
        --check) MODE=check ;;
        --install-tools) INSTALL_TOOLS=1 ;;
        --ccx) shift; GIVEN_CCX="${1:-}" ;;
        --ccx=*) GIVEN_CCX="${1#--ccx=}" ;;
        -h|--help) sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        -*) echo "unknown option: $1" >&2; exit 2 ;;
        *) INSTALL_FOLDER="$1" ;;
    esac
    shift
done

BREW_PREFIX="${PPM_BREW_PREFIX:-}"
if [ -z "$BREW_PREFIX" ]; then
    if [ -d /opt/homebrew ]; then
        BREW_PREFIX=/opt/homebrew
    elif [ -x /usr/local/bin/brew ]; then
        BREW_PREFIX=/usr/local
    else
        BREW_PREFIX=/opt/homebrew        # the default of an Apple Silicon Mac, so the messages below name real paths
    fi
fi
# The PATH a non-interactive shell does not have. The .NET SDK of the dotnet-install script lives in ~/.dotnet.
export PATH="$BREW_PREFIX/bin:/usr/local/bin:$HOME/.dotnet:$PATH"

# Before the check: the prerequisites of the list below are macOS prerequisites, and a Linux shell that gets the list
# would not know that this script is the wrong tool for it
if [ "$(uname -s)" != Darwin ]; then
    echo "This script builds the macOS app; on $(uname -s) use ./build.sh Release and ./run.sh Release." >&2
    exit 2
fi

MISSING=()       # what is missing, in the words of the check
PACKAGES=()      # Homebrew formulas that bring it
MANUAL=()        # what Homebrew does not install
HINTS=()         # what to do once the missing parts are there

ok()      { printf '  ok        %s\n' "$1"; }
missing() { printf '  MISSING   %s\n' "$1"; MISSING+=("$1"); }
package() { PACKAGES+=("$2"); missing "$1 (brew install $2)"; }
manual()  { MANUAL+=("$1"); missing "$1"; }
hint()    { HINTS+=("$1"); }

python_version() {          # the x.y.z of a python3, or nothing when it does not run
    "$1" -c 'import sys; print("%d.%d.%d" % sys.version_info[:3])' 2>/dev/null
}
python_new_enough() {       # 3.12 is the first Python whose tarfile.extractall takes filter=, which bundle-deps.py uses
    "$1" -c 'import sys; sys.exit(0 if sys.version_info >= (3, 12) else 1)' > /dev/null 2>&1
}
find_ccx() {                # the way PrePoMax looks for CalculiX at start (SolverSettings.cs FindCalculixExecutable)
    if [ -n "$GIVEN_CCX" ]; then echo "$GIVEN_CCX"; return; fi
    for directory in "$BREW_PREFIX/bin" /usr/local/bin; do
        if [ -f "$directory/ccx" ]; then echo "$directory/ccx"; return; fi
        newest=$(ls -1 "$directory"/ccx_* 2>/dev/null | tail -1)
        if [ -n "$newest" ]; then echo "$newest"; return; fi
    done
    if command -v ccx > /dev/null 2>&1; then command -v ccx; return; fi
    echo ""
}
find_dotnet() {             # build.sh looks for the SDK the same way: one in the PATH may be older
    for candidate in "$BREW_PREFIX/bin/dotnet" /usr/local/share/dotnet/dotnet "$HOME/.dotnet/dotnet" "$(command -v dotnet 2>/dev/null)"; do
        if [ -n "$candidate" ] && [ -x "$candidate" ] && "$candidate" --list-sdks 2>/dev/null | grep -q "^10\."; then
            echo "$candidate"; return
        fi
    done
    echo ""
}

check_all() {
    MISSING=(); PACKAGES=(); MANUAL=(); HINTS=()
    echo "PrePoMax for macOS - build prerequisites"
    echo "  Homebrew: $BREW_PREFIX"
    echo

    if [ -n "$(command -v xcode-select 2>/dev/null)" ] && xcode-select -p > /dev/null 2>&1; then
        ok "Xcode command line tools ($(xcode-select -p))"
    else
        manual "Xcode command line tools: run xcode-select --install (clang builds the native parts)"
    fi

    if command -v cmake > /dev/null 2>&1; then
        ok "cmake ($(command -v cmake))"
    else
        package "cmake" "cmake"
    fi

    if [ -d "$BREW_PREFIX/opt/opencascade" ]; then
        ok "opencascade ($BREW_PREFIX/opt/opencascade)"
    else
        package "opencascade" "opencascade"
    fi

    if [ -f "$BREW_PREFIX/lib/libgmsh.dylib" ]; then
        ok "gmsh ($BREW_PREFIX/lib/libgmsh.dylib)"
    else
        package "gmsh" "gmsh"
    fi

    if [ -f "$BREW_PREFIX/lib/liblpsolve55.dylib" ]; then
        ok "lp_solve ($BREW_PREFIX/lib/liblpsolve55.dylib)"
    else
        package "lp_solve" "lp_solve"
    fi

    # 3.12 or newer, and not the 3.9 that macOS ships: bundle-deps.py unpacks the Python of the app with it
    PYTHON=""
    seen=" "
    for candidate in "$BREW_PREFIX/bin/python3" /usr/local/bin/python3 "$(command -v python3 2>/dev/null)" /usr/bin/python3; do
        if [ -z "$candidate" ] || [ ! -x "$candidate" ]; then continue; fi
        case "$seen" in *" $candidate "*) continue ;; esac
        seen="$seen$candidate "
        version=$(python_version "$candidate")
        if [ -z "$version" ]; then
            continue
        elif python_new_enough "$candidate"; then
            ok "python $version ($candidate)"
            [ -n "$PYTHON" ] || PYTHON="$candidate"
        else
            printf '  too old   python %s (%s, bundle-deps.py needs 3.12 or newer)\n' "$version" "$candidate"
        fi
    done
    if [ -z "$PYTHON" ]; then
        package "a Python 3.12 or newer for bundle-deps.py" "python"
        hint "start this script again after brew install, so that it picks up the new $BREW_PREFIX/bin/python3 (an interactive shell does that by itself)"
    fi

    ccx=$(find_ccx)
    if [ -n "$ccx" ]; then
        if [ -x "$ccx" ]; then ok "CalculiX ($ccx)"; else manual "CalculiX: $ccx is not executable"; fi
    else
        package "CalculiX ccx for the app's Solver" "costerwi/calculix/calculix-ccx"
        hint "or point this script at a ccx that is already there: ./setup-build.sh --ccx /path/to/ccx"
    fi

    dotnet=$(find_dotnet)
    if [ -n "$dotnet" ]; then
        ok ".NET 10 SDK ($dotnet)"
    else
        manual ".NET 10 SDK: https://dotnet.microsoft.com/download/dotnet/10.0 (or the dotnet-install script into ~/.dotnet)"
    fi
}

report() {
    echo
    if [ ${#MISSING[@]} -eq 0 ]; then
        echo "All prerequisites are there."
        for line in "${HINTS[@]}"; do echo "Hint: $line"; done
        return 0
    fi
    echo "Missing:"
    for line in "${MISSING[@]}"; do echo "  - $line"; done
    if [ ${#PACKAGES[@]} -gt 0 ]; then
        echo
        echo "One brew command installs the packages:"
        echo "  brew install ${PACKAGES[*]}"
    fi
    if [ ${#MANUAL[@]} -gt 0 ]; then
        echo
        echo "By hand:"
        for line in "${MANUAL[@]}"; do echo "  - $line"; done
    fi
    if [ ${#HINTS[@]} -gt 0 ]; then
        echo
        for line in "${HINTS[@]}"; do echo "Hint: $line"; done
    fi
    return 1
}

check_all
if ! report; then
    if [ "$INSTALL_TOOLS" = 1 ] && [ ${#PACKAGES[@]} -gt 0 ]; then
        if ! command -v brew > /dev/null 2>&1; then
            echo
            echo "brew is not in the PATH; install Homebrew from https://brew.sh first." >&2
            exit 1
        fi
        echo
        echo "brew install ${PACKAGES[*]}"
        if ! brew install "${PACKAGES[@]}"; then
            echo "brew install failed; see its output above." >&2
            exit 1
        fi
        echo
        check_all
        report || exit 1
    else
        echo
        if [ "$INSTALL_TOOLS" = 1 ]; then
            echo "Nothing to install with Homebrew; the missing parts above need a hand." >&2
        else
            echo "./setup-build.sh --install-tools runs the brew command above." >&2
        fi
        exit 1
    fi
fi

echo

if [ "$MODE" = check ]; then
    echo "(no build: --check)"
    exit 0
fi

echo "Building and installing PrePoMax.app ..."
BUILD_ARGS=(--standalone)
if [ -n "$GIVEN_CCX" ]; then BUILD_ARGS+=(--ccx "$GIVEN_CCX"); fi
if [ -n "$INSTALL_FOLDER" ]; then BUILD_ARGS+=("$INSTALL_FOLDER"); fi
exec "$HERE/make-app.sh" "${BUILD_ARGS[@]}"