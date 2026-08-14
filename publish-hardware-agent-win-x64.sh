#!/usr/bin/env bash

set -euo pipefail

script_dir=$(dirname "$(realpath "$0")")
project="$script_dir/src/backend/hardware/Fabric.Hardware.Agent/Fabric.Hardware.Agent.csproj"
artifacts_dir="$script_dir/artifacts/hardware-agent"
publish_dir="$artifacts_dir/win-x64/publish"
zip_path="$artifacts_dir/fabric-hardware-agent-win-x64.zip"

if ! command -v zip >/dev/null 2>&1; then
  printf 'zip command not found. Install zip first.\n' >&2
  exit 1
fi

printf 'Cleaning %s\n' "$artifacts_dir"
rm -rf "$artifacts_dir"
mkdir -p "$publish_dir"

printf 'Publishing Fabric.Hardware.Agent for win-x64 (self-contained)\n'
dotnet publish "$project" \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=false \
  -p:PublishReadyToRun=false \
  -o "$publish_dir"

printf 'Creating %s\n' "$zip_path"
rm -f "$zip_path"
(
  cd "$publish_dir"
  zip -r "$zip_path" .
)

printf 'Done.\nPublish dir: %s\nZip: %s\n' "$publish_dir" "$zip_path"
