#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat <<'TEXT'
Usage: tools/build-appimage.sh [options]

Publishes the desktop gallery with NativeAOT and packs it as an AppImage in
artifacts/appimage/. The AppImage contains the native shim for its
architecture. It uses the Union and Qt of the system it runs on, and the
managed engine when the system has no compatible Union.

The shim is the one the native job of CI built for this checkout,
downloaded with tools/natives/fetch-natives.sh, which needs the GitHub CLI.

Options:
  --rid <rid>        linux-x64 or linux-arm64. The default is the RID of this
                     computer. NativeAOT builds only for the RID of this
                     computer.
  --revision <n>     The fourth number of the version. The default is 0.
  --shim <file>      Use this shim, for example native/build/libavaunion-native-*.so.
  --no-native        Leave the shim out.
  --out <dir>        The output folder. The default is artifacts/appimage.
TEXT
}

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APPIMAGETOOL_VERSION=1.9.1

case "$(uname -m)" in
    x86_64) HOST_RID=linux-x64 ;;
    aarch64) HOST_RID=linux-arm64 ;;
    *) echo "no AppImage for $(uname -m)" >&2; exit 1 ;;
esac

RID=$HOST_RID
REVISION=0
NATIVE=1
SHIM=""
OUT="$ROOT/artifacts/appimage"

while [ $# -gt 0 ]; do
    case "$1" in
        --rid) RID=${2:?}; shift 2 ;;
        --revision) REVISION=${2:?}; shift 2 ;;
        --shim) SHIM=${2:?}; shift 2 ;;
        --no-native) NATIVE=0; shift ;;
        --out) OUT=${2:?}; shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) echo "unknown argument '$1'" >&2; usage >&2; exit 1 ;;
    esac
done

case "$RID" in
    linux-x64) ARCH=x86_64 ;;
    linux-arm64) ARCH=aarch64 ;;
    *) echo "unknown RID '$RID'" >&2; exit 1 ;;
esac
if [ "$RID" != "$HOST_RID" ]; then
    echo "NativeAOT cannot build $RID on $HOST_RID" >&2
    exit 1
fi

PROJECT="$ROOT/samples/AvaUnion.Gallery.Desktop"
WORK="$ROOT/artifacts/appimage-work/$RID"
APPDIR="$WORK/AvaUnion-Gallery.AppDir"
rm -rf "$WORK"
mkdir -p "$APPDIR/usr/bin" "$OUT"

UNION_VERSION="$(dotnet msbuild "$PROJECT" -getProperty:AvaUnionUnionVersion)"
VERSION="$UNION_VERSION"
if [ "$REVISION" != 0 ]; then
    VERSION="$UNION_VERSION.$REVISION"
fi

dotnet publish "$PROJECT" -c Release -r "$RID" -p:PublishAot=true \
    -p:AvaUnionRevision="$REVISION" -o "$WORK/publish"

rm -f "$WORK"/publish/libavaunion-native-*.so
cp -a "$WORK"/publish/. "$APPDIR/usr/bin/"
rm -f "$APPDIR"/usr/bin/*.dbg "$APPDIR"/usr/bin/*.pdb "$APPDIR"/usr/bin/*.xml

if [ "$NATIVE" -eq 1 ]; then
    if [ -z "$SHIM" ]; then
        "$ROOT/tools/natives/fetch-natives.sh" --rid "$RID"
        SHIM="$ROOT/native/artifacts/$RID/libavaunion-native-$UNION_VERSION.so"
    fi
    if [ ! -f "$SHIM" ]; then
        echo "$SHIM does not exist" >&2
        exit 1
    fi
    cp "$SHIM" "$APPDIR/usr/bin/libavaunion-native-$UNION_VERSION.so"
fi

cp "$PROJECT/AppImage/AppRun" "$APPDIR/AppRun"
cp "$PROJECT/AppImage/avaunion-gallery.desktop" "$PROJECT/AppImage/avaunion-gallery.svg" "$APPDIR/"
ln -s avaunion-gallery.svg "$APPDIR/.DirIcon"

TOOL="$(command -v appimagetool || true)"
if [ -z "$TOOL" ]; then
    TOOL="$ROOT/artifacts/tools/appimagetool-$APPIMAGETOOL_VERSION-$(uname -m).AppImage"
    if [ ! -x "$TOOL" ]; then
        mkdir -p "$(dirname "$TOOL")"
        curl -fsSL -o "$TOOL" \
            "https://github.com/AppImage/appimagetool/releases/download/$APPIMAGETOOL_VERSION/appimagetool-$(uname -m).AppImage"
        chmod +x "$TOOL"
    fi
fi

TARGET="$OUT/AvaUnion-Gallery-$VERSION-$ARCH.AppImage"
APPIMAGE_EXTRACT_AND_RUN=1 ARCH=$ARCH VERSION=$VERSION "$TOOL" --no-appstream "$APPDIR" "$TARGET"
echo "$TARGET"
