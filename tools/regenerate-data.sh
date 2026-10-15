#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
regenerate-data.sh [OPTIONS]

Rebuilds everything derived from the installed Union:

  tests/Data/snapshot/*.json          rules and schemes exported by union-export
  tests/Data/parity/chains.json       the query corpus (tools/union-corpus)
  tests/Data/parity/expected-native.json
                                      the corpus through the real ElementQuery
  src/AvaUnion.Theme/Snapshot/breeze.union.bin
                                      the managed engine's snapshot
  tests/Data/styles/avaunion-test.json, .union.bin
  tests/Data/parity/expected-app.json the test style package, its snapshot and
                                      the corpus through it
  tests/Data/styles/avaunion-demo.json
  samples/AvaUnion.Gallery/Styles/avaunion-demo.union.bin
                                      the gallery's demo style and its snapshot

  --union-source DIR   a Union source tree, default external/union
  --colors DIR         where BreezeLight.colors and BreezeDark.colors live,
                       default /usr/share/color-schemes
  --build DIR          the shim's build tree, default native/build
  --export-tool FILE   a union-export that is already built; the shim
                       is then not built
TEXT
}

union_source=
colors=/usr/share/color-schemes
build="$root/native/build"
export_tool=

while [ $# -gt 0 ]; do
    case "$1" in
        --union-source) union_source=${2:?}; shift 2 ;;
        --colors) colors=${2:?}; shift 2 ;;
        --build) build=${2:?}; shift 2 ;;
        --export-tool) export_tool=${2:?}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; exit 1 ;;
    esac
done

cmake_args=(-S "$root/native" -B "$build" -DCMAKE_BUILD_TYPE=Release)
if [ -n "$union_source" ]; then
    cmake_args+=(-DUNION_SOURCE_DIR="$union_source")
fi
if command -v ninja >/dev/null; then
    cmake_args+=(-G Ninja)
fi
if [ -z "$export_tool" ]; then
    [ -f "$build/CMakeCache.txt" ] || cmake "${cmake_args[@]}"
    cmake --build "$build"
    export_tool="$build/union-export"
fi
data="$root/tests/Data"
mkdir -p "$data/snapshot" "$data/parity"

export QT_QPA_PLATFORM=offscreen
scratch="$(mktemp -d)"
trap 'rm -rf "$scratch"' EXIT
export XDG_CONFIG_HOME="$scratch/config"
mkdir -p "$XDG_CONFIG_HOME"

for style in breeze breeze-rtl breeze-mobile; do
    "$export_tool" style "$style" "$data/snapshot/$style.json"
done
for scheme in BreezeLight BreezeDark; do
    "$export_tool" scheme "$colors/$scheme.colors" "$data/snapshot/$scheme.json"
done

(cd "$root" && dotnet run tools/union-corpus/union-corpus.cs)
"$export_tool" parity "$data/parity/chains.json" "$data/parity/expected-native.json" \
    "$colors/BreezeLight.colors" "$colors/BreezeDark.colors"
(cd "$root" && dotnet run tools/union-snapshot/union-snapshot.cs)

"$export_tool" package "$data/styles/avaunion-test" "$data/styles/avaunion-test.json"
(cd "$root" && dotnet run tools/union-snapshot/union-snapshot.cs -- \
    --export "$data/styles/avaunion-test.json" --out "$data/styles/avaunion-test.union.bin")
AVU_PARITY_PACKAGES="$data/styles/avaunion-test" "$export_tool" parity \
    "$data/parity/chains.json" "$data/parity/expected-app.json" \
    "$colors/BreezeLight.colors" "$colors/BreezeDark.colors"

demo="$root/samples/AvaUnion.Gallery/Styles"
if [ -d "$demo/avaunion-demo" ]; then
    "$export_tool" package "$demo/avaunion-demo" "$data/styles/avaunion-demo.json"
    (cd "$root" && dotnet run tools/union-snapshot/union-snapshot.cs -- \
        --export "$data/styles/avaunion-demo.json" --out "$demo/avaunion-demo.union.bin")
fi
