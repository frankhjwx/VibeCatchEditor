#!/bin/bash
set -euo pipefail
source "$(cd "$(dirname "$0")" && pwd)/macos-dotnet.sh"
VCE_ARCH="$(uname -m)"
case "$VCE_ARCH" in arm64) VCE_RID=osx-arm64 ;; x86_64) VCE_RID=osx-x64 ;; *) echo 'Unsupported Mac architecture'; exit 1 ;; esac
VCE_APP="$VCE_ROOT/artifacts/macos/VibeCatchEditor.app"
mkdir -p "$VCE_APP/Contents/MacOS"
"$VCE_DOTNET" publish "$VCE_ROOT/src/VibeCatchEditor.Mac" -c Release -r "$VCE_RID" --self-contained true -o "$VCE_APP/Contents/MacOS" -p:NuGetLockFilePath=packages.publish.lock.json
cat > "$VCE_APP/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
<key>CFBundleExecutable</key><string>VibeCatchEditor.Mac</string>
<key>CFBundleIdentifier</key><string>io.github.frankhjwx.VibeCatchEditor</string>
<key>CFBundleName</key><string>VibeCatchEditor</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleShortVersionString</key><string>0.1.0</string>
<key>CFBundleVersion</key><string>1</string>
<key>LSMinimumSystemVersion</key><string>12.0</string>
<key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST
codesign --force --deep --sign - "$VCE_APP"
echo "$VCE_APP"
