#!/usr/bin/env bash

set -euo pipefail

script_dir=$(dirname "$(realpath "$0")")
frontend_dir="$script_dir/src/frontend"
backend_project="$script_dir/src/backend/Fabric.Server/Fabric.Server.csproj"
backend_wwwroot="$script_dir/src/backend/Fabric.Server/wwwroot"
frontend_dist="$frontend_dir/dist"

if [ $# -ne 1 ]; then
  printf 'Usage: %s <version>\n' "$0" >&2
  exit 1
fi

input_version="$1"
version="${input_version#v}"

if [[ ! "$version" =~ ^([0-9]+)\.([0-9]+)\.([0-9]+)([-+][0-9A-Za-z.-]+)?$ ]]; then
  printf 'Version must be semver, example: 1.2.3 or 1.2.3-beta.1\n' >&2
  exit 1
fi

assembly_version="${BASH_REMATCH[1]}.${BASH_REMATCH[2]}.${BASH_REMATCH[3]}.0"

printf 'Building frontend for version %s\n' "$version"
npm install --prefix "$frontend_dir"
npm run build --prefix "$frontend_dir"

printf 'Syncing frontend dist to backend wwwroot\n'
rm -rf "$backend_wwwroot"
mkdir -p "$backend_wwwroot"
cp -R "$frontend_dist"/. "$backend_wwwroot"

printf 'Publishing backend container %s\n' "$version"
dotnet publish "$backend_project" \
  -c Release \
  /t:PublishContainer \
  -p:ContainerImageTags="$version" \
  -p:Version="$version" \
  -p:InformationalVersion="$version" \
  -p:AssemblyVersion="$assembly_version" \
  -p:FileVersion="$assembly_version"
