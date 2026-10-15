#!/usr/bin/env bash

set -euo pipefail

usage() {
    cat <<'TEXT'
Usage: tools/natives/fetch-natives.sh [options]

Downloads the native shims that the native job of CI built for the
recipe key of this checkout. The shims go to native/artifacts/<rid>/.
AvaUnion.Native packs them from there. The script needs the GitHub CLI (gh).

Options:
  --rid <rid>   Download this RID (linux-x64 or linux-arm64).
                The default is the RID of this computer.
  --all         Download all RIDs.
  --repo <o/n>  The repository to download from. The default is
                $AVAUNION_NATIVES_REPO, then $GITHUB_REPOSITORY, then origin.
  --key <key>   Use this recipe key, not the key of this checkout.
  --force       Download again when the local files have the same key.
  --list        Show the recipe key and the artifacts, then stop.
TEXT
}

ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
ARTIFACTS="$ROOT/native/artifacts"
STAMP_DIR="$ARTIFACTS/.natives"
ALL_RIDS=(linux-x64 linux-arm64)
REPO="${AVAUNION_NATIVES_REPO:-${GITHUB_REPOSITORY:-}}"
KEY=""
RIDS=()
WANT_ALL=false
FORCE=false
LIST_ONLY=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --rid) RIDS+=("$2"); shift 2 ;;
        --all) WANT_ALL=true; shift ;;
        --repo) REPO="$2"; shift 2 ;;
        --key) KEY="$2"; shift 2 ;;
        --force) FORCE=true; shift ;;
        --list) LIST_ONLY=true; shift ;;
        -h|--help) usage; exit 0 ;;
        *) echo "Unknown option: $1" >&2; exit 1 ;;
    esac
done

if ! command -v gh >/dev/null 2>&1; then
    echo "Error: this script needs the GitHub CLI (gh). See https://cli.github.com" >&2
    exit 1
fi

if [ -z "$REPO" ]; then
    REPO="$(git -C "$ROOT" remote get-url origin | sed -E 's#^(https://github.com/|git@github.com:)##; s#\.git$##')"
fi

if [ -z "$KEY" ]; then
    KEY="$("$ROOT/tools/natives/recipe-key.sh")"
    recipe_files=()
    while IFS= read -r f; do recipe_files+=("$f"); done < <("$ROOT/tools/natives/recipe-key.sh" --files)
    if ! git -C "$ROOT" diff --quiet -- "${recipe_files[@]}"; then
        echo "Warning: you have unstaged changes to the native recipe." >&2
        echo "         The downloaded shims will not include them. Build native/ instead." >&2
    fi
fi

artifact_name() { echo "avaunion-native-$1-$KEY"; }

find_run() {
    gh api "repos/$REPO/actions/artifacts?name=$1&per_page=100" \
        --jq '[.artifacts[] | select(.expired | not)] | max_by(.created_at) | .workflow_run.id // empty'
}

host_rid() {
    case "$(uname -m)" in
        x86_64) echo "linux-x64" ;;
        aarch64|arm64) echo "linux-arm64" ;;
        *) echo "Error: unsupported host $(uname -m)" >&2; return 1 ;;
    esac
}

if [ "$LIST_ONLY" = true ]; then
    echo "repo:       $REPO"
    echo "recipe key: $KEY"
    echo "artifacts:"
    for rid in "${ALL_RIDS[@]}"; do
        name="$(artifact_name "$rid")"
        run_id="$(find_run "$name")"
        printf '  %-48s %s\n' "$name" "${run_id:+run $run_id}${run_id:-missing}"
    done
    exit 0
fi

if [ "$WANT_ALL" = true ]; then
    RIDS=("${ALL_RIDS[@]}")
elif [ ${#RIDS[@]} -eq 0 ]; then
    RIDS=("$(host_rid)")
fi

TMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TMP_DIR"' EXIT
mkdir -p "$STAMP_DIR"

echo "Fetching native shims for recipe $KEY from $REPO"
fetched=0
skipped=0
for rid in "${RIDS[@]}"; do
    stamp="$STAMP_DIR/$rid"
    if [ "$FORCE" != true ] && [ -f "$stamp" ] && [ "$(cat "$stamp")" = "$KEY" ]; then
        echo "  $rid: up to date"
        skipped=$((skipped + 1))
        continue
    fi

    name="$(artifact_name "$rid")"
    run_id="$(find_run "$name")"
    if [ -z "$run_id" ]; then
        echo "Error: there is no $name artifact in $REPO." >&2
        echo "       Run CI on this branch, or build native/ locally." >&2
        exit 1
    fi

    echo "  $rid: downloading $name (run $run_id)"
    gh run download "$run_id" --repo "$REPO" --name "$name" --dir "$TMP_DIR/$rid"
    rm -rf "${ARTIFACTS:?}/$rid"
    mkdir -p "$ARTIFACTS/$rid"
    cp "$TMP_DIR/$rid"/libavaunion-native-*.so "$ARTIFACTS/$rid/"

    echo "$KEY" > "$stamp"
    fetched=$((fetched + 1))
done

echo "Done: $fetched downloaded, $skipped already current -> $ARTIFACTS"
