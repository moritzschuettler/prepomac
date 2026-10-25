#!/bin/bash
# Builds PrePoMax.app from the Release build and installs it in /Applications (or ~/Applications when /Applications is
# not writable): ./make-app.sh [--standalone] [install folder]
# The app opens from the Launchpad, the Dock or Finder like any other; the command line program stays
# Contents/MacOS/PrePoMax.com inside it. With --standalone the app carries the Homebrew libraries, CalculiX and a
# Python of its own (bundle-deps.py), so that it runs on a Mac without Homebrew, as the downloadable release does.
set -e
cd "$(dirname "$0")"
STANDALONE=0
if [ "$1" = "--standalone" ]; then STANDALONE=1; shift; fi
if [ "$STANDALONE" = 1 ]; then
    # The .NET runtime goes into the app as well
    ./build.sh Release -r osx-arm64 --self-contained
    OUT=PrePoMax/bin/Release/net10.0/osx-arm64
else
    ./build.sh Release
    OUT=PrePoMax/bin/Release/net10.0
fi
TARGET=${1:-/Applications}
[ -w "$TARGET" ] || TARGET="$HOME/Applications"
mkdir -p "$TARGET"
APP="$TARGET/PrePoMax.app"
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
[ "$STANDALONE" = 1 ] && python3 bundle-deps.py "$STAGE"
# The build signs its programs ad hoc; the bundle is signed again as a whole
codesign --force --deep --sign - "$STAGE" > /dev/null 2>&1 || true
rm -rf "${APP:?}"
mv "$STAGE" "$APP"
# Finder and the Launchpad pick up the new bundle
/System/Library/Frameworks/CoreServices.framework/Frameworks/LaunchServices.framework/Support/lsregister -f "$APP" || true
echo "Installed $APP"
