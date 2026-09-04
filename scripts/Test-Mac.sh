#!/bin/bash
set -euo pipefail
source "$(cd "$(dirname "$0")" && pwd)/macos-dotnet.sh"
if [[ "${1:-}" != --native-only ]]; then
    for suite in Core Gameplay; do
        "$VCE_DOTNET" run --project "$VCE_ROOT/tests/VibeCatchEditor.$suite.Tests" -c Release
    done
    "$VCE_DOTNET" run --project "$VCE_ROOT/tests/VibeCatchEditor.Formats.Tests" -c Release -- --skip-external-fixtures
    for suite in App Skinning SkinArchive; do
        "$VCE_DOTNET" run --project "$VCE_ROOT/macOS/tests/$suite" -c Release
    done
fi
if [[ "${1:-}" == --skip-device-tests ]]; then
    echo 'SKIP Mac native device tests (explicitly excluded on headless CI)'
else
    "$VCE_DOTNET" run --project "$VCE_ROOT/tests/VibeCatchEditor.Mac.Tests" -c Release
fi
