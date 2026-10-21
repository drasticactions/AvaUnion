#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
update-breeze-icons.sh [OPTIONS]

Packs every Breeze icon, at every size that a breeze-icons checkout has, into
src/AvaUnion.BreezeIcons/Icons/breeze-icons.bin, with tools/breeze-icons.
Symbolic links are followed. The license file of the icons is copied too. It
fails when a name in src/AvaUnion.BreezeIcons/icons.txt is missing.

  --tag TAG     breeze-icons tag to clone, default v6.31.0
  --source DIR  use this breeze-icons checkout instead of cloning
  --check       only report whether the archive is up to date
  -h            this help
TEXT
}

tag=v6.31.0 source="" check=""
while [ $# -gt 0 ]; do
    case "$1" in
        --tag) tag=${2:?}; shift 2 ;;
        --source) source=${2:?}; shift 2 ;;
        --check) check=--check; shift ;;
        -h|--help) usage; exit 0 ;;
        *) usage >&2; exit 2 ;;
    esac
done

if [ -z "$source" ]; then
    work="$(mktemp -d)"
    trap 'rm -rf "$work"' EXIT
    git clone -q --depth 1 --branch "$tag" https://invent.kde.org/frameworks/breeze-icons.git "$work/breeze-icons"
    source="$work/breeze-icons"
fi

version="$(git -C "$source" describe --tags --always 2>/dev/null || echo "$tag") $(git -C "$source" rev-parse --short HEAD 2>/dev/null || echo unknown)"
cd "$root"
dotnet run tools/breeze-icons/breeze-icons.cs -- --source "$source" --version "$version" $check
