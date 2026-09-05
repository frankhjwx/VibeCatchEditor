#!/bin/bash
set -euo pipefail
source "$(cd "$(dirname "$0")" && pwd)/scripts/macos-dotnet.sh"
[[ "$(uname -s)" == Darwin ]] || { echo 'Use Run-Editor.cmd on Windows.'; exit 1; }
"$VCE_DOTNET" build "$VCE_ROOT/src/VibeCatchEditor.Mac/VibeCatchEditor.Mac.csproj" -c Release --nologo --verbosity minimal -p:RestoreLockedMode=true
exec "$VCE_DOTNET" "$VCE_ROOT/src/VibeCatchEditor.Mac/bin/Release/net8.0/VibeCatchEditor.Mac.dll" "$@"
