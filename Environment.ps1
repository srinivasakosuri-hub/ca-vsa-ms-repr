# -------------------------------
# CONFIGURATION
# -------------------------------
$containerName = "sqlserver"
$saPassword = "YourStrong*Password12"
$dbName = "TemplateMsDb"
$scriptFolder = "C:\Source\ca-vsa-ms\src\infrastructure\TemplateMs.Database\dbo\Tables"

# -----------------------------------------
# START OR REUSE CONTAINER
# -----------------------------------------
$containerExists = docker ps -a --format "{{.Names}}" | Select-String $containerName

if (-not $containerExists) {
  Write-Host "Container does not exist. Creating..."
  docker run -d `
    --name $containerName `
    -e "ACCEPT_EULA=Y" `
    -e "SA_PASSWORD=$saPassword" `
    -p 1433:1433 `
    mcr.microsoft.com/mssql/server:2022-latest
}
else {
  Write-Host "Container exists. Starting..."
  docker start $containerName | Out-Null
}

# -----------------------------------------
# WAIT FOR SQL SERVER TO BE READY
# -----------------------------------------
Write-Host "Waiting for SQL Server to accept connections..."
$ready = $false

for ($i = 1; $i -le 30; $i++) {
  $result = docker run --rm `
    --network host `
    mcr.microsoft.com/mssql-tools `
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $saPassword -Q "SELECT 1" 2>$null

  if ($result) {
    $ready = $true
    break
  }

  Start-Sleep -Seconds 2
}

if (-not $ready) {
  Write-Host "SQL Server did not start in time."
  exit 1
}

Write-Host "SQL Server is ready."

# -----------------------------------------
# CREATE DATABASE IF NOT EXISTS
# -----------------------------------------
docker run --rm `
  --network host `
  mcr.microsoft.com/mssql-tools `
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $saPassword -Q "IF DB_ID('$dbName') IS NULL CREATE DATABASE [$dbName];"

Write-Host "Database check complete."

# -----------------------------------------
# RUN ALL SCRIPTS IN FOLDER
# -----------------------------------------
$files = Get-ChildItem -Path $scriptFolder -Filter *.sql

foreach ($file in $files) {
  Write-Host "Executing: $($file.Name)"

  docker run --rm `
    --network host `
    -v "${scriptFolder}:/scripts" `
    mcr.microsoft.com/mssql-tools `
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P $saPassword -d $dbName -i "/scripts/$($file.Name)"
}

Write-Host "All scripts executed successfully."
