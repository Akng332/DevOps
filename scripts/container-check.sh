#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
bash scripts/container-setup.sh
python3 scripts/check-compose.py

docker compose build app
docker compose up -d db migrator app

wait_healthy() {
    local service="$1" limit="${2:-120}" container state
    container="$(docker compose ps -q "$service")"
    for ((i=0; i<limit; i+=2)); do
        state="$(docker inspect --format '{{.State.Health.Status}}' "$container" 2>/dev/null || true)"
        if [[ "$state" == healthy ]]; then return 0; fi
        if [[ "$state" == unhealthy ]]; then
            docker compose logs --tail=80 "$service" >&2
            return 1
        fi
        sleep 2
    done
    echo "$service did not become healthy" >&2
    docker compose logs --tail=80 "$service" >&2
    return 1
}

wait_healthy db
wait_healthy app
[[ "$(docker compose exec -T app id -u)" == 1000 ]]
curl --fail --silent http://127.0.0.1:8080/api/health >/dev/null
echo 'App and database are healthy; application runs as uid 1000'

marker="lab4_$(date +%s)"
docker compose exec -T db psql -U postgres -d musicstore -v ON_ERROR_STOP=1 \
    -c 'CREATE TABLE IF NOT EXISTS public.lab4_persistence_probe (marker text PRIMARY KEY);' >/dev/null
docker compose exec -T db psql -U postgres -d musicstore -v ON_ERROR_STOP=1 \
    -c "INSERT INTO public.lab4_persistence_probe VALUES ('$marker');" >/dev/null

docker compose up -d --build --force-recreate db migrator app
wait_healthy db
wait_healthy app
found="$(docker compose exec -T db psql -U postgres -d musicstore -Atqc \
    "SELECT count(*) FROM public.lab4_persistence_probe WHERE marker = '$marker'")"
[[ "$found" == 1 ]]
echo 'Data survived container and application image recreation'

backup_file="/backups/musicstore-lab4-$(date +%Y%m%d-%H%M%S).dump"
docker compose --profile tools run --rm db-tools /scripts/container-backup.sh "$backup_file"

docker compose --profile restore up -d restore-db
wait_healthy restore-db
docker compose --profile tools run --rm db-tools /scripts/container-restore.sh "$backup_file"
restored="$(docker compose exec -T restore-db psql -U postgres -d musicstore -Atqc \
    "SELECT count(*) FROM public.lab4_persistence_probe WHERE marker = '$marker'")"
[[ "$restored" == 1 ]]
echo 'Backup restored successfully into a separate clean volume'

docker compose stop app
[[ "$(docker inspect --format '{{.State.ExitCode}}' "$(docker compose ps -aq app)")" == 0 ]]
docker compose up -d app
wait_healthy app
echo 'Application stopped cleanly and restarted healthy'
echo 'Container check passed'
