#!/bin/bash
# Builds PrePoMax.app from the Release build and installs it: ./make-app.sh [--standalone] [--ccx <ccx>] [install folder]
#
# The app opens from the Launchpad, the Dock or Finder like any other; the command line program stays
# Contents/MacOS/PrePoMax.com inside it. With --standalone the app carries the Homebrew libraries, CalculiX and a
# Python of its own (bundle-deps.py), so that it runs on a Mac without Homebrew, as the downloadable release does.
#
# The staged bundle is checked with verify-bundle.sh before anything is installed: an app without Solver, Python or
# Frameworks does not fail at the build but at the first solve. Without --standalone the app uses the gmsh, lp_solve
# and CalculiX of this Mac, and the check demands that they are there.
#
# ./setup-build.sh checks the tools first and then runs this script with --standalone.
set -e
cd "$(dirname "$0")"

# A shell that is not an interactive login shell (ssh, an IDE, .NET's MSBuild) has Homebrew's bin and the SDK of the
# dotnet-install script ($HOME/.dotnet) not in its PATH; cmake would be missing in the native build then
BREW_PREFIX="${PPM_BREW_PREFIX:-}"
if [ -z "$BREW_PREFIX" ]; then
    if [ -d /opt/homebrew ]; then
        BREW_PREFIX=/opt/homebrew
    elif [ -x /usr/local/bin/brew ]; then
        BREW_PREFIX=/usr/local
    else
        BREW_PREFIX=/opt/homebrew
    fi
fi
export PATH="$BREW_PREFIX/bin:/usr/local/bin:$HOME/.dotnet:$PATH"

STANDALONE=0
GIVEN_CCX=""
TARGET_ARG=""
while [ $# -gt 0 ]; do
    case "$1" in
        --standalone) STANDALONE=1 ;;
        --ccx) shift; GIVEN_CCX="${1:-}" ;;
        --ccx=*) GIVEN_CCX="${1#--ccx=}" ;;
        -h|--help) sed -n '2,/^$/p' "$0" | sed 's/^# \{0,1\}//'; exit 0 ;;
        -*) echo "unknown option: $1" >&2; exit 2 ;;
        *) TARGET_ARG="$1" ;;
    esac
    shift
done
if [ -n "$GIVEN_CCX" ]; then
    if [ ! -x "$GIVEN_CCX" ]; then
        echo "--ccx: $GIVEN_CCX is not executable" >&2
        exit 1
    fi
    # bundle-deps.py copies this CalculiX into Contents/MacOS/Solver
    export PPM_CCX="$GIVEN_CCX"
fi

# bundle-deps.py unpacks the Python of the app with tarfile.extractall(filter=...), which Python 3.12 added; the
# /usr/bin/python3 of macOS is 3.9 and ends with "TypeError: extractall() got an unexpected keyword argument 'filter'"
find_python() {
    for candidate in "$BREW_PREFIX/bin/python3" /usr/local/bin/python3 "$(command -v python3 2>/dev/null)" /usr/bin/python3; do
        if [ -n "$candidate" ] && [ -x "$candidate" ] && \
                "$candidate" -c 'import sys; sys.exit(0 if sys.version_info >= (3, 12) else 1)' > /dev/null 2>&1; then
            echo "$candidate"
            return 0
        fi
    done
    return 1
}

if [ "$STANDALONE" = 1 ]; then
    # The .NET runtime goes into the app as well
    ./build.sh Release -r osx-arm64 --self-contained
    OUT=PrePoMax/bin/Release/net10.0/osx-arm64
else
    ./build.sh Release
    OUT=PrePoMax/bin/Release/net10.0
fi
if [ ! -d "$OUT" ]; then
    echo "the build did not produce $OUT" >&2
    exit 1
fi

# An older PrePoMax.app is replaced where it is. A new one in the other folder would be a second app with the same
# bundle identifier, and the Launchpad and the Dock would keep opening the old one.
choose_target() {
    if [ -n "$TARGET_ARG" ]; then echo "$TARGET_ARG"; return; fi
    if [ -d "$HOME/Applications/PrePoMax.app" ]; then echo "$HOME/Applications"; return; fi
    if [ -w "/Applications" ]; then echo "/Applications"; return; fi
    echo "$HOME/Applications"
}
TARGET=$(choose_target)
mkdir -p "$TARGET"
APP="$TARGET/PrePoMax.app"
OTHER=""
if [ "$TARGET" != "/Applications" ] && [ -d "/Applications/PrePoMax.app" ]; then OTHER="/Applications/PrePoMax.app"; fi
if [ "$TARGET" != "$HOME/Applications" ] && [ -d "$HOME/Applications/PrePoMax.app" ]; then OTHER="$HOME/Applications/PrePoMax.app"; fi

STAGE=$(mktemp -d)/PrePoMax.app
mkdir -p "$STAGE/Contents/MacOS" "$STAGE/Contents/Resources"
cp -R "$OUT"/. "$STAGE/Contents/MacOS/"
# PrePoMax finds its files next to itself, as run.sh starts it from its folder
cat > "$STAGE/Contents/MacOS/PrePoMaxLauncher" <<'EOF'
#!/bin/bash
cd "$(dirname "$0")"
exec ./PrePoMax "$@"
EOF
chmod +x "$STAGE/Contents/MacOS/PrePoMaxLauncher"
cp Native/AppIcon.icns "$STAGE/Contents/Resources/AppIcon.icns"
# The license texts of PrePoMax and of the components the app carries or links
cp -R ../Licenses "$STAGE/Contents/Resources/Licenses"
cp ../LICENSE "$STAGE/Contents/Resources/Licenses/LICENSE"
VERSION=$(git -C .. describe --tags --always 2>/dev/null || echo 3.0)
cat > "$STAGE/Contents/Info.plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>PrePoMax</string>
    <key>CFBundleDisplayName</key><string>PrePoMax</string>
    <key>CFBundleIdentifier</key><string>com.prepomax.mac</string>
    <key>CFBundleExecutable</key><string>PrePoMaxLauncher</string>
    <key>CFBundleIconFile</key><string>AppIcon</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleShortVersionString</key><string>3.0</string>
    <key>CFBundleVersion</key><string>$VERSION</string>
    <key>LSMinimumSystemVersion</key><string>13.0</string>
    <key>LSApplicationCategoryType</key><string>public.app-category.productivity</string>
    <key>NSHighResolutionCapable</key><true/>
</dict>
</plist>
EOF
if [ "$STANDALONE" = 1 ]; then
    if ! PYTHON=$(find_python); then
        echo "bundle-deps.py needs Python 3.12 or newer, and none was found." >&2
        echo "  brew install python" >&2
        echo "(the /usr/bin/python3 of macOS is 3.9 and cannot unpack the Python that goes into the app)" >&2
        exit 1
    fi
    "$PYTHON" bundle-deps.py "$STAGE"
fi

# Nothing is installed before the bundle is complete. A missing Solver, Python or Frameworks costs nothing at the build
# and everything at the first solve.
INCOMPLETE=0
if [ "$STANDALONE" = 1 ]; then
    ./verify-bundle.sh "$STAGE" --standalone || INCOMPLETE=1
else
    ./verify-bundle.sh "$STAGE" || INCOMPLETE=1
fi
if [ "$INCOMPLETE" != 0 ]; then
    echo
    if [ -d "$APP" ]; then
        echo "Not installed: $APP was left as it is." >&2
    else
        echo "Not installed: $APP was not written." >&2
    fi
    echo "The incomplete bundle stays for a look: $STAGE" >&2
    exit 1
fi

# The build signs its programs ad hoc; the bundle is signed again as a whole
if command -v codesign > /dev/null 2>&1; then
    codesign --force --deep --sign - "$STAGE" > /dev/null 2>&1 || true
fi
rm -rf "${APP:?}"
mv "$STAGE" "$APP"
# Finder and the Launchpad pick up the new bundle
if [ -x /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister ]; then
    /System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "$APP" || true
fi
echo "Installed $APP ($(du -sm "$APP" | cut -f1) MB)"
if [ -n "$OTHER" ]; then
    echo
    echo "A second copy is in $OTHER, and the Launchpad and the Dock may still open that one. Remove it:"
    echo "  rm -rf \"$OTHER\""
fi
