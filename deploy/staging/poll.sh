#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "$0")" && pwd)"
exec 8>"$root/.poll.lock"
flock -n 8 || exit 0

run="$(curl --fail --silent --show-error --max-time 15 \
    'https://api.github.com/repos/Akng332/DevOps/actions/runs?branch=lab6%2Fcd&event=push&per_page=1')"
read -r sha status conclusion < <(python3 -c '
import json,sys
run=json.load(sys.stdin)["workflow_runs"][0]
print(run["head_sha"], run["status"], run["conclusion"])
' <<< "$run")
[[ "$status" == completed && "$conclusion" == success ]] || exit 0

image="ghcr.io/akng332/devops:sha-$sha"
[[ "$(cat "$root/.last-good-image" 2>/dev/null || true)" != "$image" ]] || exit 0
[[ "$(cat "$root/.failed-image" 2>/dev/null || true)" != "$image" ]] || exit 0

source_dir="$root/source"
git -C "$source_dir" fetch --quiet --depth=1 origin lab6/cd
[[ "$(git -C "$source_dir" rev-parse FETCH_HEAD)" == "$sha" ]] || {
    echo 'Branch moved while fetching; retry later' >&2
    exit 1
}
git -C "$source_dir" checkout --quiet --detach "$sha"
cp "$source_dir/deploy/staging/compose.yaml" "$root/compose.yaml"
cp "$source_dir/deploy/staging/deploy.sh" "$root/deploy.sh"
cp -a "$source_dir/database/." "$root/database/"
cp -a "$source_dir/scripts/." "$root/scripts/"

if bash "$root/deploy.sh" "$image"; then
    rm -f "$root/.failed-image"
else
    printf '%s\n' "$image" > "$root/.failed-image"
    exit 1
fi
