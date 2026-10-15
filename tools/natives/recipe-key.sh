#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/../.."

RECIPE_FILES=(
  native/CMakeLists.txt
  native/cmake
  native/src
  native/tools
  tools/natives/build-union.sh
  tools/natives/build-shim.sh
  external/union
)

if [[ ${1:-} == --files ]]; then
  printf '%s\n' "${RECIPE_FILES[@]}"
  exit 0
fi

git ls-files -s -- "${RECIPE_FILES[@]}" | git hash-object --stdin | cut -c1-16
