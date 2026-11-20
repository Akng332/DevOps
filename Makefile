DOTNET ?= dotnet
SOLUTION := MusicStore.sln
BACKUP_FILE ?= backups/musicstore.dump

.PHONY: setup run format test sast db-check verify migrate backup restore

setup:
	$(DOTNET) restore $(SOLUTION)
	python3 -m venv .venv
	.venv/bin/python -m pip install -r requirements-dev.txt

run:
	$(DOTNET) run --project src/MusicStore.Api/MusicStore.Api.csproj

format:
	$(DOTNET) format $(SOLUTION) --verify-no-changes --severity warn

test:
	$(DOTNET) test $(SOLUTION) --configuration Release -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura -p:Threshold=40 -p:ThresholdType=line

sast:
	.venv/bin/semgrep scan --config p/csharp --config p/security-audit --error src

db-check:
	bash scripts/check-db.sh

verify: format test sast db-check

migrate:
	bash scripts/migrate.sh

backup:
	bash scripts/backup.sh $(BACKUP_FILE)

restore:
	bash scripts/restore.sh $(BACKUP_FILE)
