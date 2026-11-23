#!/usr/bin/env bash
set -Eeuo pipefail

cd "$(dirname "$0")"
candidate="${1:?Usage: deploy.sh ghcr.io/owner/repo:sha-FULL_SHA}"
[[ "$candidate" =~ ^ghcr\.io/[a-z0-9_-]+/[a-z0-9_-]+:sha-([0-9a-f]{40})$ ]] || {
    echo 'Expected a GHCR image with a full commit SHA tag' >&2
    exit 2
}
expected="${BASH_REMATCH[1]}"

exec 9>.deploy.lock
flock -n 9 || { echo 'Another deployment is running' >&2; exit 3; }
previous="$(cat .last-good-image 2>/dev/null || true)"

rollback() {
    local result=$?
    trap - ERR
    if [[ -n "$previous" ]]; then
        echo "Release failed; rolling back to $previous" >&2
        IMAGE_REF="$previous" docker compose up -d --no-build --force-recreate app || true
    else
        echo 'First release failed; no previous image to restore' >&2
    fi
    exit "$result"
}
trap rollback ERR

test -s secrets/postgres_password
test -s secrets/app_db_password
test -s secrets/jwt_key

echo "Pulling $candidate"
docker pull "$candidate"
IMAGE_REF="$candidate" docker compose up -d --no-build --force-recreate db migrator app

app_id="$(IMAGE_REF="$candidate" docker compose ps -q app)"
for ((i=0; i<60; i++)); do
    state="$(docker inspect --format '{{.State.Health.Status}}' "$app_id")"
    [[ "$state" == healthy ]] && break
    [[ "$state" != unhealthy ]] || { echo 'Application is unhealthy' >&2; false; }
    sleep 2
done
[[ "$state" == healthy ]] || { echo 'Application health timed out' >&2; false; }

base=http://192.168.56.10:18080
curl --fail --silent --show-error "$base/api/health" | python3 -c 'import json,sys; assert json.load(sys.stdin)["status"] == "Healthy"'
version="$(curl --fail --silent --show-error "$base/version" | python3 -c 'import json,sys; print(json.load(sys.stdin)["version"])')"
[[ "$version" == "$expected" ]] || { echo "Version mismatch: $version" >&2; false; }
curl --fail --silent --show-error "$base${SMOKE_PATH:-/api/discs}" | python3 -c 'import json,sys; assert isinstance(json.load(sys.stdin), list)'

printf '%s\n' "$candidate" > .last-good-image
trap - ERR
echo "Release $expected passed health, version and catalog checks"
