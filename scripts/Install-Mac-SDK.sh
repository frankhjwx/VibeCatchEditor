#!/bin/bash
set -euo pipefail
VCE_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
[[ "$(uname -s)" == Darwin ]] || { echo 'This installer is for macOS.'; exit 1; }
mkdir -p "$VCE_ROOT/artifacts/dotnet"
curl --fail --location https://dot.net/v1/dotnet-install.sh -o "$VCE_ROOT/artifacts/dotnet-install.sh"
bash "$VCE_ROOT/artifacts/dotnet-install.sh" --version 8.0.419 --install-dir "$VCE_ROOT/artifacts/dotnet" --no-path
