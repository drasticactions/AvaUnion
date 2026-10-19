#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"

usage() {
    cat <<'TEXT'
publish-local.sh [OPTIONS]

Packs the AvaUnion packages at a fixed local
version and installs them into the nuget cache on this machine, so another
solution on the same box restores them without a source of its own.

  --version V   version to stamp, default 9999.0.0-localbuild
  --cache DIR   global packages folder, default what dotnet reports
  --out DIR     where the packages are packed, default artifacts/packages
  -n            print what would be published and stop
  --no-native   don't build the shim; pack whatever is in native/artifacts
                (tools/natives/fetch-natives.sh puts CI's shims there)

The shim is built against the installed Union with CMake (Union's source
tree is external/union unless UNION_SOURCE_DIR is set) and put in
native/artifacts/<rid>/, where AvaUnion.Native packs it from; the other
architecture's comes from CI (tools/natives/fetch-natives.sh, which needs gh).

The version is fixed, so each run deletes the extracted copy in the cache
first. Without that a republish is invisible to a restore.
TEXT
}

global_packages() {
    dotnet nuget locals global-packages --list |
        sed -n 's|.*global-packages: *||p' |
        head -1 |
        sed 's|/*$||'
}

lower() {
    printf '%s' "$1" | tr '[:upper:]' '[:lower:]'
}

ids=(AvaUnion.Native AvaUnion.Theme AvaUnion.DataGrid AvaUnion.Controls AvaUnion.BreezeIcons)
version=9999.0.0-localbuild
cache=
out=
dry=0
native=1

while [ $# -gt 0 ]; do
    case "$1" in
        --version) version=${2:?--version needs a value}; shift 2 ;;
        --cache) cache=${2:?--cache needs a value}; shift 2 ;;
        --out) out=${2:?--out needs a value}; shift 2 ;;
        -n|--dry-run) dry=1; shift ;;
        --no-native) native=0; shift ;;
        -h|--help) usage; exit 0 ;;
        *)
            echo "unknown argument '$1'" >&2
            exit 1
            ;;
    esac
done

: "${out:=$root/artifacts/packages}"
: "${cache:=$(global_packages)}"

if [ -z "$cache" ]; then
    echo "dotnet nuget locals global-packages --list named no folder" >&2
    exit 1
fi

echo "version $version"
echo "cache   $cache"

if [ "$dry" -eq 1 ]; then
    printf '  %s\n' "${ids[@]}"
    exit 0
fi

if [ "$native" -eq 1 ]; then
    case "$(uname -m)" in
        x86_64) rid=linux-x64 ;;
        aarch64) rid=linux-arm64 ;;
        *) echo "no shim for $(uname -m)" >&2; exit 1 ;;
    esac
    build="$root/native/build"
    cmake_args=(-S "$root/native" -B "$build" -DCMAKE_BUILD_TYPE=Release)
    if [ -n "${UNION_SOURCE_DIR:-}" ]; then
        cmake_args+=(-DUNION_SOURCE_DIR="$UNION_SOURCE_DIR")
    fi
    [ -f "$build/CMakeCache.txt" ] || cmake "${cmake_args[@]}"
    cmake --build "$build"
    mkdir -p "$root/native/artifacts/$rid"
    cp "$build"/libavaunion-native-*.so "$root/native/artifacts/$rid/"
    echo "shim    $(ls "$root/native/artifacts/$rid")"
    other=linux-arm64
    [ "$rid" = linux-arm64 ] && other=linux-x64
    "$root/tools/natives/fetch-natives.sh" --rid "$other"
fi

mkdir -p "$out" "$cache"
out=$(cd "$out" && pwd)
cache=$(cd "$cache" && pwd)

rm -f "$out"/*."$version".nupkg "$out"/*."$version".snupkg

for id in "${ids[@]}"; do
    dotnet pack "$root/src/$id/$id.csproj" -c Release --nologo -v quiet \
        -p:Version="$version" -o "$out"
done

folder_version=$(lower "$version")

for id in "${ids[@]}"; do
    if [ ! -f "$out/$id.$version.nupkg" ]; then
        echo "$id did not pack at $version into $out" >&2
        exit 1
    fi

    installed="$cache/$(lower "$id")/$folder_version"
    if [ -d "$installed" ]; then
        rm -rf "$installed"
        echo "  cleared $installed"
    fi
done

work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

cat > "$work/nuget.config" <<XML
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="avaunion-local" value="$out" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
XML

{
    echo '<Project Sdk="Microsoft.NET.Sdk">'
    echo '  <PropertyGroup>'
    echo '    <TargetFramework>net10.0</TargetFramework>'
    echo '    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>'
    echo '    <NuGetAudit>false</NuGetAudit>'
    echo '  </PropertyGroup>'
    echo '  <ItemGroup>'
    for id in "${ids[@]}"; do
        echo "    <PackageDownload Include=\"$id\" Version=\"[$version]\" />"
    done
    echo '  </ItemGroup>'
    echo '</Project>'
} > "$work/install.csproj"

dotnet restore "$work/install.csproj" --packages "$cache" --nologo -v quiet

for id in "${ids[@]}"; do
    installed="$cache/$(lower "$id")/$folder_version"
    if [ ! -f "$installed/.nupkg.metadata" ]; then
        echo "$id $version did not install into $installed" >&2
        exit 1
    fi

    echo "published $id $version"
done
