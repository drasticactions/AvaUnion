#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat <<'TEXT'
Usage: tools/natives/build-shim.sh <dist>

Builds the native shim and union-export against the installed Union, checks
that the shim exports only avu_* symbols, and copies both into <dist>.
TEXT
}

if [ $# -eq 1 ] && { [ "$1" = -h ] || [ "$1" = --help ]; }; then
    usage
    exit 0
fi
if [ $# -ne 1 ]; then
    usage >&2
    exit 1
fi

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
DIST="$1"

cmake -S "$ROOT/native" -B "$ROOT/native/build" -G Ninja -DCMAKE_BUILD_TYPE=Release
cmake --build "$ROOT/native/build"
if nm -D --defined-only "$ROOT"/native/build/libavaunion-native-*.so | grep -v ' avu_'; then
    echo "symbols other than avu_* are exported" >&2
    exit 1
fi
mkdir -p "$DIST"
cp "$ROOT"/native/build/libavaunion-native-*.so "$ROOT/native/build/union-export" "$DIST/"
