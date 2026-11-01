#Requires -Version 5.1
<#
.SYNOPSIS
    Starts MedConnect infrastructure and local app services.

.DESCRIPTION
    1. docker compose up -d  (Postgres, Keycloak, Seq, MongoDB, RabbitMQ)
    2. Waits until Keycloak realm is ready
    3. Starts AppointmentService, CommunicationService, NotificationService, and SPA in separate windows

.EXAMPLE
    .\scripts\start-all.ps1
    .\scripts\start-all.ps1 -Stop
    .\scripts\start-all.ps1 -SkipSpa
#>
[CmdletBinding(DefaultParameterSetName = "Start")]
param(
    [Parameter(ParameterSetName = "Stop")]
    [switch]$Stop,

    [Parameter(ParameterSetName = "Start")]
    [switch]$SkipDocker,

    [Parameter(ParameterSetName = "Start")]
    [switch]$SkipSpa,

    [Parameter(ParameterSetName = "Start")]
    [switch]$SkipWait,

    [Parameter(ParameterSetName = "Start")]
    [int]$KeycloakTimeoutSec = 180
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$Root = Split-Path -Parent $PSScriptRoot
$PidsFile = Join-Path $env:TEMP "medconnect-dev.pids.json"

$AppointmentDir = Join-Path $Root "src\Services\AppointmentService"
$CommunicationDir = Join-Path $Root "src\Services\CommunicationService"
$NotificationDir = Join-Path $Root "src\Services\NotificationService"
$WebDir = Join-Path $Root "src\Web.Generated"

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Ok {
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor Green
}

function Write-WarnLine {
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor Yellow
}

function Get-TrackedPids {
    if (-not (Test-Path $PidsFile)) {
        return @()
    }

    try {
        $data = Get-Content -Path $PidsFile -Raw -ErrorAction Stop | ConvertFrom-Json
        if ($null -eq $data) {
            return @()
        }
        return @($data)
    }
    catch {
        return @()
    }
}

function Save-TrackedPids {
    param([object[]]$Entries)
    $Entries | ConvertTo-Json | Set-Content -Path $PidsFile -Encoding UTF8
}

function Stop-TrackedApps {
    $entries = Get-TrackedPids
    foreach ($entry in $entries) {
        $procId = [int]$entry.Pid
        if ($procId -le 0) {
            continue
        }

        $proc = Get-Process -Id $procId -ErrorAction SilentlyContinue
        if ($null -eq $proc) {
            continue
        }

        Write-Host "    Stopping $($entry.Name) (PID $procId)..."
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue

        Get-CimInstance Win32_Process -Filter "ParentProcessId = $procId" -ErrorAction SilentlyContinue |
            ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    }

    if (Test-Path $PidsFile) {
        Remove-Item $PidsFile -Force
    }
}

function Test-Command {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Wait-TcpPort {
    param(
        [string]$TargetHost,
        [int]$Port,
        [int]$TimeoutSec,
        [string]$Label
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $iar = $client.BeginConnect($TargetHost, $Port, $null, $null)
            $ok = $iar.AsyncWaitHandle.WaitOne(1000, $false)
            if ($ok -and $client.Connected) {
                $client.EndConnect($iar)
                $client.Close()
                Write-Ok "$Label is listening on ${TargetHost}:${Port}"
                return
            }
            $client.Close()
        }
        catch {
            # keep polling
        }

        Start-Sleep -Seconds 2
    }

    throw "Timed out waiting for $Label on ${TargetHost}:${Port}"
}

function Wait-Http {
    param(
        [string]$Url,
        [int]$TimeoutSec,
        [string]$Label
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 500) {
                Write-Ok "$Label is ready ($Url)"
                return
            }
        }
        catch {
            # keep polling
        }

        Start-Sleep -Seconds 3
    }

    throw "Timed out waiting for $Label at $Url"
}

function Start-AppWindow {
    param(
        [string]$Name,
        [string]$WorkingDirectory,
        [string]$Command
    )

    $title = "MedConnect $Name"
    $encoded = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes(
        "`$Host.UI.RawUI.WindowTitle = '$title'; Set-Location -LiteralPath '$WorkingDirectory'; $Command"
    ))

    $proc = Start-Process -FilePath "powershell.exe" -PassThru -ArgumentList @(
        "-NoExit",
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-EncodedCommand", $encoded
    )

    return [pscustomobject]@{
        Name = $Name
        Pid  = $proc.Id
    }
}

if ($Stop) {
    Write-Step "Stopping local app windows"
    Stop-TrackedApps
    Write-Ok "Local app processes stopped"

    Write-Step "Stopping Docker Compose stack"
    Push-Location $Root
    try {
        docker compose stop
    }
    finally {
        Pop-Location
    }

    Write-Ok "Done"
    exit 0
}

Write-Step "Checking prerequisites"
$missing = @()
if (-not $SkipDocker -and -not (Test-Command "docker")) { $missing += "Docker" }
if (-not (Test-Command "dotnet")) { $missing += ".NET SDK (dotnet)" }
if (-not $SkipSpa -and -not (Test-Command "npm")) { $missing += "Node.js (npm)" }
if ($missing.Count -gt 0) {
    throw "Missing tools: $($missing -join ', ')"
}
Write-Ok "Required tools are available"

Write-Step "Checking ASP.NET Core HTTPS developer certificate"
$certOutput = & dotnet dev-certs https --check --trust 2>&1 | Out-String
if ($LASTEXITCODE -ne 0 -or $certOutput -notmatch "trusted") {
    Write-WarnLine "Dev HTTPS certificate is missing or not trusted."
    Write-WarnLine "gRPC between CommunicationService and AppointmentService will fail."
    Write-WarnLine "Fix once:  dotnet dev-certs https --clean; dotnet dev-certs https --trust"
}
else {
    Write-Ok "HTTPS developer certificate is trusted"
}

Write-Step "Checking Keycloak Admin API secret (AppointmentService user secrets)"
$secrets = & dotnet user-secrets list --project $AppointmentDir 2>&1 | Out-String
if ($secrets -notmatch "Keycloak:AdminClientSecret") {
    Write-WarnLine "Keycloak:AdminClientSecret is not set. Demo user seed / admin API will fail."
    Write-WarnLine "After Keycloak starts: Clients -> medconnect-admin-cli -> Credentials"
    Write-WarnLine "Then:  dotnet user-secrets set `"Keycloak:AdminClientSecret`" `"<secret>`" --project src\Services\AppointmentService"
}
else {
    Write-Ok "Keycloak:AdminClientSecret is present in user secrets"
}

if (-not $SkipDocker) {
    Write-Step "Starting Docker Compose (infrastructure only)"
    Push-Location $Root
    try {
        docker compose up -d
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose up failed"
        }
    }
    finally {
        Pop-Location
    }
    Write-Ok "Compose stack is starting"
}

if (-not $SkipWait -and -not $SkipDocker) {
    Write-Step "Waiting for infrastructure"
    Wait-TcpPort -TargetHost "127.0.0.1" -Port 5432 -TimeoutSec 60 -Label "Postgres"
    Wait-TcpPort -TargetHost "127.0.0.1" -Port 27017 -TimeoutSec 60 -Label "MongoDB"
    Wait-TcpPort -TargetHost "127.0.0.1" -Port 5672 -TimeoutSec 90 -Label "RabbitMQ"
    Wait-Http -Url "http://localhost:8080/realms/medconnect" -TimeoutSec $KeycloakTimeoutSec -Label "Keycloak realm"
}

Stop-TrackedApps | Out-Null

$started = @()

Write-Step "Starting AppointmentService"
$started += Start-AppWindow -Name "AppointmentService" -WorkingDirectory $AppointmentDir -Command @(
    "`$env:ASPNETCORE_ENVIRONMENT = 'Development';",
    "dotnet run --no-launch-profile --urls 'https://localhost:7246;http://localhost:5067'"
) -join " "

Write-Step "Starting CommunicationService"
$started += Start-AppWindow -Name "CommunicationService" -WorkingDirectory $CommunicationDir -Command @(
    "`$env:ASPNETCORE_ENVIRONMENT = 'Development';",
    "dotnet run --no-launch-profile --urls 'https://localhost:7280;http://localhost:5080'"
) -join " "

Write-Step "Starting NotificationService"
$started += Start-AppWindow -Name "NotificationService" -WorkingDirectory $NotificationDir -Command @(
    "`$env:ASPNETCORE_ENVIRONMENT = 'Development';",
    "dotnet run --no-launch-profile --urls 'https://localhost:7290;http://localhost:5090'"
) -join " "

    if (-not $SkipSpa) {
        Write-Step "Starting SPA"
        Write-Ok "npm install will sync dependencies before Vite starts"
        $started += Start-AppWindow -Name "SPA" -WorkingDirectory $WebDir -Command "npm install; npm run dev"
    }

Save-TrackedPids -Entries $started

Write-Host ""
Write-Host "Services:" -ForegroundColor Cyan
Write-Host "  Postgres              localhost:5432"
Write-Host "  Keycloak              http://localhost:8080"
Write-Host "  Seq                   http://localhost:5341"
Write-Host "  MongoDB               localhost:27017"
Write-Host "  RabbitMQ              localhost:5672  UI http://localhost:15672"
Write-Host "  AppointmentService    http://localhost:5067  https://localhost:7246"
Write-Host "  CommunicationService  http://localhost:5080  https://localhost:7280"
Write-Host "  NotificationService   http://localhost:5090  https://localhost:7290"
if (-not $SkipSpa) {
    Write-Host "  SPA                   http://localhost:3000"
}
Write-Host ""
Write-Host "Stop everything:  .\scripts\start-all.ps1 -Stop" -ForegroundColor DarkGray
Write-Host "App logs are in the opened PowerShell windows." -ForegroundColor DarkGray
