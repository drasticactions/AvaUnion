#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
update-breeze-icons.sh [OPTIONS]

Copies the Breeze icons named in src/AvaUnion.BreezeIcons/icons.txt from a
breeze-icons checkout into src/AvaUnion.BreezeIcons/Icons/<size>/<name>.svg,
at the sizes 16, 22, 32 and 48 that the checkout has. Symbolic links are
followed. The license files of the icons are copied too.

  --tag TAG     breeze-icons tag to clone, default v6.31.0
  --source DIR  use this breeze-icons checkout instead of cloning
  -h            this help
TEXT
}

tag=v6.31.0 source=""
while [ $# -gt 0 ]; do
    case "$1" in
        --tag) tag=${2:?}; shift 2 ;;
        --source) source=${2:?}; shift 2 ;;
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

out="$root/src/AvaUnion.BreezeIcons/Icons"
rm -rf "$out"
mkdir -p "$out"
missing=0
while read -r name; do
    [ -z "$name" ] && continue
    found=0
    for size in 16 22 32 48; do
        file="$(find "$source/icons" -path "*/$size/$name.svg" | sort | head -n 1)"
        if [ -n "$file" ]; then
            mkdir -p "$out/$size"
            cp -L "$file" "$out/$size/$name.svg"
            found=1
        fi
    done
    if [ "$found" = 0 ]; then
        echo "missing: $name" >&2
        missing=1
    fi
done < "$root/src/AvaUnion.BreezeIcons/icons.txt"

cp "$source/COPYING-ICONS" "$out/COPYING-ICONS"
echo "$(git -C "$source" describe --tags --always 2>/dev/null || echo "$tag") $(git -C "$source" rev-parse --short HEAD 2>/dev/null || echo unknown)" > "$out/VERSION"
exit "$missing"
