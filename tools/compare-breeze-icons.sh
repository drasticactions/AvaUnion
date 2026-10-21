#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
compare-breeze-icons.sh [OPTIONS]

Draws every Breeze icon with AvaUnion's SVG reader and with a reference
renderer, at four times its size (at most 512 pixels), and compares them.
<out>/report.tsv lists the share of differing pixels per icon, worst first;
<out>/diff/ has the reference and AvaUnion side by side for each icon over
the limit.

  --renderer NAME  qt (QtSvg, as Plasma draws icons; built from
                   tools/svg-reference), resvg or rsvg; default qt
  --tag TAG        breeze-icons tag to clone, default v6.31.0
  --source DIR     use this breeze-icons checkout instead of cloning
  --limit N        report icons whose share is over N, default 0.02
  --out DIR        output folder, default artifacts/breeze-compare
  -h               this help
TEXT
}

renderer=qt tag=v6.31.0 source="" limit=0.02 out="$root/artifacts/breeze-compare"
while [ $# -gt 0 ]; do
    case "$1" in
        --renderer) renderer=${2:?}; shift 2 ;;
        --tag) tag=${2:?}; shift 2 ;;
        --source) source=${2:?}; shift 2 ;;
        --limit) limit=${2:?}; shift 2 ;;
        --out) out=${2:?}; shift 2 ;;
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

rm -rf "$out"
mkdir -p "$out/svg" "$out/ref"
jobs="$out/jobs.txt"
: > "$jobs"
declare -A seen
while IFS= read -r file; do
    size="$(basename "$(dirname "$file")")"
    name="$(basename "$file" .svg)"
    [[ "$size" =~ ^[0-9]+$ ]] || continue
    [ -n "${seen[$size/$name]:-}" ] && continue
    seen[$size/$name]=1
    mkdir -p "$out/svg/$size" "$out/ref/$size"
    cp -L "$file" "$out/svg/$size/$name.svg"
    pixels=$((size * 4))
    [ "$pixels" -gt 512 ] && pixels=512
    echo "$pixels $out/svg/$size/$name.svg $out/ref/$size/$name.png" >> "$jobs"
done < <(find "$source/icons" -name '*.svg' | LC_ALL=C sort)

case "$renderer" in
    qt)
        build="$root/tools/svg-reference/build"
        [ -f "$build/CMakeCache.txt" ] || cmake -S "$root/tools/svg-reference" -B "$build" -DCMAKE_BUILD_TYPE=Release
        cmake --build "$build"
        split -n l/"$(nproc)" "$jobs" "$out/jobs."
        ls "$out"/jobs.?? | xargs -P "$(nproc)" -I{} "$build/svg-reference" {}
        rm "$out"/jobs.??
        ;;
    resvg) xargs -P "$(nproc)" -L 1 sh -c 'resvg -w "$0" -h "$0" "$1" "$2"' < "$jobs" ;;
    rsvg) xargs -P "$(nproc)" -L 1 sh -c 'rsvg-convert -w "$0" -h "$0" "$1" -o "$2"' < "$jobs" ;;
    *) echo "unknown renderer '$renderer'" >&2; exit 2 ;;
esac

AVAUNION_SVG_CORPUS="$out" AVAUNION_SVG_LIMIT="$limit" \
    dotnet run --project "$root/tests/AvaUnion.Theme.Tests" -c Release -- --filter-class AvaUnion.Theme.Tests.SvgCorpusTests
cat "$out/summary.txt"
