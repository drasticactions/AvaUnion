#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
shoot.sh [OPTIONS]

Renders every gallery page to PNG with the headless platform and Skia (no
display needed): <out>/<page>.png.

  --engine NAME   native or managed, default: whatever Auto picks
  --scheme NAME   switch to this color scheme after the first frame
  --style ID      use this style, for example avaunion-demo
  --size WxH      window size, default 900x700
  --scale S       render scaling, default 1
  --rtl           lay the windows out right to left
  --out DIR       output folder, default artifacts/shots
  -h              this help
TEXT
}

rtl="" engine="" scheme="" scale="" style="" size=900x700 out="$root/artifacts/shots"
while [ $# -gt 0 ]; do
    case "$1" in
        --engine) engine=${2:?}; shift 2 ;;
        --scheme) scheme=${2:?}; shift 2 ;;
        --size) size=${2:?}; shift 2 ;;
        --scale) scale=${2:?}; shift 2 ;;
        --style) style=${2:?}; shift 2 ;;
        --rtl) rtl=1; shift ;;
        --out) out=${2:?}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; exit 1 ;;
    esac
done

if [ -n "$engine" ]; then
    export AVAUNION_ENGINE="$engine"
fi

args=(--shoot "$out" "$size")
if [ -n "$scheme" ]; then
    args+=("$scheme")
fi
if [ -n "$scale" ]; then
    args+=(--scale "$scale")
fi
if [ -n "$style" ]; then
    args+=(--style "$style")
fi
if [ -n "$rtl" ]; then
    args+=(--rtl)
fi

dotnet run --project "$root/samples/AvaUnion.Gallery.Desktop" -c Release -- "${args[@]}"
