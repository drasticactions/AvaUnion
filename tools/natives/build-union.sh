#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat <<'TEXT'
Usage: tools/natives/build-union.sh [options]

Builds Union from the external/union submodule and installs it.

Options:
  --prefix <dir>   The install prefix. The default is /usr.
  --build <dir>    The build folder. The default is a temporary folder.
TEXT
}

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
SOURCE="$ROOT/external/union"
PREFIX=/usr
BUILD=""

while [ $# -gt 0 ]; do
    case "$1" in
        --prefix) PREFIX=${2:?}; shift 2 ;;
        --build) BUILD=${2:?}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; usage >&2; exit 1 ;;
    esac
done

if [ ! -f "$SOURCE/CMakeLists.txt" ]; then
    echo "external/union is empty; run git submodule update --init" >&2
    exit 1
fi
if [ -z "$BUILD" ]; then
    BUILD="$(mktemp -d)"
fi

cmake -S "$SOURCE" -B "$BUILD" -G Ninja -DCMAKE_BUILD_TYPE=Release \
    -DCMAKE_INSTALL_PREFIX="$PREFIX" -DBUILD_TESTING=OFF
cmake --build "$BUILD"
cmake --install "$BUILD"
