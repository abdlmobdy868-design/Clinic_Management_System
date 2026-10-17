cd $PSScriptRoot
$proj = Get-ChildItem -Recurse -Filter ClinicManagementSystem.csproj | Select-Object -First 1 -ExpandProperty FullName
if (-not $proj) { Write-Error "Project file not found."; exit 1 }
Write-Host "Project: $proj"
# Remove Migrations folder if exists
$migFolder = Join-Path (Split-Path $proj) "Migrations"
if (Test-Path $migFolder) { Remove-Item -Recurse -Force $migFolder }
# Drop database
dotnet ef database drop --force --project "$proj" --startup-project "$proj"
# Add migration
dotnet ef migrations add FixAllFkErrors --project "$proj" --startup-project "$proj" -o Migrations
# Update database
dotnet ef database update --project "$proj" --startup-project "$proj"
Write-Host "Done."