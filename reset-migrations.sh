#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
server_dir="$repo_root/src/backend/Fabric.Server"
solution="$repo_root/src/backend/Fabric.slnx"

cd "$server_dir"

migration_dirs=(
  "Tenants/Persistence/Migrations"
  "AccessPolicies/Persistence/Migrations"
  "Identities/Persistence/Migrations"
  "Employees/Persistence/Migrations"
  "CredentialManagement/Persistence/Migrations"
  "Visitors/Persistence/Migrations"
  "Sagas/Persistence/Migrations"
  "Locations/Persistence/Migrations"
  "Reception/Persistence/Migrations"
  "Kiosk/Persistence/Migrations"
)

for dir in "${migration_dirs[@]}"; do
  mkdir -p "$dir"
  find "$dir" -type f -name "*.cs" -delete
done

dotnet build "$solution" /bl:{}

dotnet ef migrations add TenantsInit --context TenantsDbContext --output-dir Tenants/Persistence/Migrations --no-build
dotnet ef migrations add AccessPoliciesInit --context AccessPoliciesDbContext --output-dir AccessPolicies/Persistence/Migrations --no-build
dotnet ef migrations add IdentitiesInit --context IdentitiesDbContext --output-dir Identities/Persistence/Migrations --no-build
dotnet ef migrations add EmployeesInit --context EmployeesDbContext --output-dir Employees/Persistence/Migrations --no-build
dotnet ef migrations add CredentialManagementInit --context CredentialManagementDbContext --output-dir CredentialManagement/Persistence/Migrations --no-build
dotnet ef migrations add VisitorsInit --context VisitorsDbContext --output-dir Visitors/Persistence/Migrations --no-build
dotnet ef migrations add SagasInit --context SagasDbContext --output-dir Sagas/Persistence/Migrations --no-build
dotnet ef migrations add LocationsInit --context LocationsDbContext --output-dir Locations/Persistence/Migrations --no-build
dotnet ef migrations add ReceptionInit --context ReceptionDbContext --output-dir Reception/Persistence/Migrations --no-build
dotnet ef migrations add KioskInit --context KioskDbContext --output-dir Kiosk/Persistence/Migrations --no-build
