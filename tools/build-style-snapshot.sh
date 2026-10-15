#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
build-style-snapshot.sh <package-dir> <out.union.bin> [--check]

Builds the managed snapshot of a Union style package with the installed
Union. Builds the shim in native/build first when it is missing.

  --check   do not write; exit 1 when <out.union.bin> is stale
TEXT
}

check=()
args=()
for arg in "$@"; do
    case "$arg" in
        --check) check=(--check) ;;
        -h|--help) usage; exit 0 ;;
        *) args+=("$arg") ;;
    esac
done
if [ ${#args[@]} -ne 2 ]; then
    usage >&2
    exit 1
fi

package="$(cd -- "${args[0]}" && pwd)"
out="$(cd -- "$(dirname -- "${args[1]}")" && pwd)/$(basename -- "${args[1]}")"
build="$root/native/build"
if [ ! -x "$build/union-export" ]; then
    cmake_args=(-S "$root/native" -B "$build" -DCMAKE_BUILD_TYPE=Release)
    if command -v ninja >/dev/null; then
        cmake_args+=(-G Ninja)
    fi
    cmake "${cmake_args[@]}"
    cmake --build "$build"
fi

scratch="$(mktemp -d)"
trap 'rm -rf "$scratch"' EXIT
QT_QPA_PLATFORM=offscreen "$build/union-export" package "$package" "$scratch/export.json"
(cd "$root" && dotnet run tools/union-snapshot/union-snapshot.cs -- \
    --export "$scratch/export.json" --out "$out" "${check[@]}")
