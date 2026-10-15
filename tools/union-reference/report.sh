#!/usr/bin/env bash
set -euo pipefail
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
here="$root/tools/union-reference"
out="$root/artifacts/reference"
scheme=""
rtl=""
while [ $# -gt 0 ]; do
    case "$1" in
        --scheme) scheme=${2:?}; shift 2 ;;
        --rtl) rtl=1; shift ;;
        --out) out=${2:?}; shift 2 ;;
        *) echo "unknown argument '$1'" >&2; exit 1 ;;
    esac
done

build="$here/build"
[ -f "$build/CMakeCache.txt" ] || cmake -S "$here" -B "$build" -DCMAKE_BUILD_TYPE=Release
cmake --build "$build"

scratch="$(mktemp -d)"
trap 'rm -rf "$scratch"' EXIT
export XDG_CONFIG_HOME="$scratch"
export QT_FORCE_STDERR_LOGGING=1
export XDG_CURRENT_DESKTOP=KDE

reference=("$build/union-reference" -platform wayland "$out/reference" "$here"/scenes/*.qml)
if [ -n "$scheme" ]; then
    reference+=(--scheme "$scheme")
fi
if [ -n "$rtl" ]; then
    reference+=(-reverse)
fi
if [ -n "${WAYLAND_DISPLAY:-}" ]; then
    "${reference[@]}"
else
    kwin_wayland --virtual --width 1600 --height 1200 --exit-with-session "${reference[*]}"
fi

"$root/tools/shoot.sh" --engine native --out "$out/avaunion" ${rtl:+--rtl} ${scheme:+--scheme "$(basename "$scheme" .colors)"}

{
    echo '<!DOCTYPE html><html><head><meta charset="utf-8"><title>Union reference</title>'
    echo '<style>body{font-family:sans-serif;margin:16px} td{vertical-align:top;padding:4px} img{max-width:640px;border:1px solid #ccc}</style></head><body>'
    echo '<h1>Union Qt Quick reference and AvaUnion</h1><table><tr><th>Reference</th><th>AvaUnion</th></tr>'
    for ref in "$out"/reference/*.png; do
        name=$(basename "$ref" .png)
        echo "<tr><td><h3>$name</h3><img src=\"reference/$name.png\"></td><td><h3>&nbsp;</h3><img src=\"avaunion/$name.png\" alt=\"no gallery page named $name\"></td></tr>"
    done
    echo '</table></body></html>'
} > "$out/index.html"
echo "$out/index.html"
