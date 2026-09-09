SHELL := /bin/bash

SOLUTION := OrkutNew.slnx
API_PROJECT := App.API/OrkutNew.csproj
MIGRATIONS_PROJECT := App.Infrastructure/App.Infrastructure.csproj
DB_CONTEXT := AppDbContext

EF := dotnet ef
EF_COMMON := --startup-project $(API_PROJECT) --project $(MIGRATIONS_PROJECT) --context $(DB_CONTEXT)

.PHONY: help restore build test test-coverage run watch \
        migration-add migration-remove migration-list migration-script migration-bundle \
        db-update db-drop

help:
	@echo "Orkut New Backend"
	@echo ""
	@echo "Usage:"
	@echo "  make restore"
	@echo "  make build"
	@echo "  make test"
	@echo "  make test-coverage"
	@echo "  make run"
	@echo "  make watch"
	@echo ""
	@echo "EF Core migrations:"
	@echo "  make migration-add NAME=InitialCreate"
	@echo "  make migration-remove"
	@echo "  make migration-list"
	@echo "  make migration-script OUTPUT=artifacts/migration.sql"
	@echo "  make migration-script FROM=InitialCreate TO=AddUsers OUTPUT=artifacts/migration.sql"
	@echo "  make migration-bundle OUTPUT=artifacts/efbundle"
	@echo ""
	@echo "Database:"
	@echo "  make db-update"
	@echo "  make db-update MIGRATION=InitialCreate"
	@echo "  make db-drop"

restore:
	dotnet restore $(SOLUTION)

build:
	dotnet build $(SOLUTION)

test:
	dotnet test $(SOLUTION)

test-coverage:
	dotnet test $(SOLUTION) --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults

run:
	dotnet run --project $(API_PROJECT)

watch:
	dotnet watch --project $(API_PROJECT)

migration-add:
	@if [[ -z "$(NAME)" ]]; then \
		echo "ERROR: migration name is required. Example: make migration-add NAME=InitialCreate"; \
		exit 1; \
	fi
	$(EF) migrations add $(NAME) $(EF_COMMON)

migration-remove:
	$(EF) migrations remove $(EF_COMMON)

migration-list:
	$(EF) migrations list $(EF_COMMON)

migration-script:
	@mkdir -p "$$(dirname "$(if $(OUTPUT),$(OUTPUT),artifacts/migration.sql)")"
	$(EF) migrations script $(FROM) $(TO) $(EF_COMMON) --output $(if $(OUTPUT),$(OUTPUT),artifacts/migration.sql)

migration-bundle:
	@mkdir -p "$$(dirname "$(if $(OUTPUT),$(OUTPUT),artifacts/efbundle)")"
	$(EF) migrations bundle $(EF_COMMON) --output $(if $(OUTPUT),$(OUTPUT),artifacts/efbundle)

db-update:
	$(EF) database update $(MIGRATION) $(EF_COMMON)

db-drop:
	$(EF) database drop $(EF_COMMON) --force
