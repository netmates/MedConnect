#Requires -Version 5.1
<#
.SYNOPSIS
    Starts MedConnect in the local Kubernetes cluster and opens the SPA.

.DESCRIPTION
    1. Refuses to start when Compose is using Postgres, MongoDB, or Keycloak volumes
    2. kubectl apply -k deploy
    3. Waits until every pod in namespace medconnect is Ready
    4. Port-forwards Keycloak, Seq, AppointmentService, CommunicationService, and NotificationService
    5. Starts the SPA in a separate window

.EXAMPLE
    .\scripts\start-k8s.ps1
    .\scripts\start-k8s.ps1 -SkipSpa
    .\scripts\start-k8s.ps1 -Stop
#>
[CmdletBinding(DefaultParameterSetName = "Start")]
param(
    [Parameter(ParameterSetName = "Stop")]
    [switch]$Stop,

    [Parameter(ParameterSetName = "Start")]
    [switch]$SkipSpa,

    [Parameter(ParameterSetName = "Start")]
    [int]$ReadyTimeoutSec = 300
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$Root = Split-Path -Parent $PSScriptRoot
$PidsFile = Join-Path $env:TEMP "medconnect-k8s.pids.json"
$WebDir = Join-Path $Root "src\Web.Generated"
$Namespace = "medconnect"

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Write-Ok {
    param([string]$Message)
    Write-Host "    $Message" -ForegroundColor Green
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

function Get-ChildProcessIds {
    param([int]$ParentId)

    $ids = @()
    $children = @(Get-CimInstance Win32_Process -Filter "ParentProcessId = $ParentId" -ErrorAction SilentlyContinue)
    foreach ($child in $children) {
        $ids += [int]$child.ProcessId
        $ids += @(Get-ChildProcessIds -ParentId $child.ProcessId)
    }

    return $ids
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
        $descendants = @(Get-ChildProcessIds -ParentId $procId)
        [array]::Reverse($descendants)
        foreach ($id in $descendants) {
            Stop-Process -Id $id -Force -ErrorAction SilentlyContinue
        }

        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }

    if (Test-Path $PidsFile) {
        Remove-Item $PidsFile -Force
    }
}

function Test-Command {
    param([string]$Name)
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Start-AppWindow {
    param(
        [string]$Name,
        [string]$WorkingDirectory,
        [string]$Command
    )

    $title = "MedConnect k8s $Name"
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

function Assert-ComposeVolumesFree {
    Push-Location $Root
    try {
        $running = @(docker compose ps --status running --format "{{.Service}}" 2>&1)
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose ps failed: $($running -join ' ')"
        }
    }
    finally {
        Pop-Location
    }

    $blocked = @($running | Where-Object { $_ -in @("postgres", "mongo", "keycloak") })
    if ($blocked.Count -gt 0) {
        throw "Compose is using shared volumes ($($blocked -join ', ')). Stop it first: docker compose stop"
    }
}

if ($Stop) {
    Write-Step "Stopping port-forward and SPA windows"
    Stop-TrackedApps
    Write-Ok "Local windows stopped. Kubernetes pods are still running."
    Write-Host "Remove the deployment:  kubectl delete -k deploy" -ForegroundColor DarkGray
    exit 0
}

Write-Step "Checking prerequisites"
foreach ($tool in @("kubectl", "docker")) {
    if (-not (Test-Command $tool)) {
        throw "Missing tool: $tool"
    }
}
if (-not $SkipSpa -and -not (Test-Command "npm")) {
    throw "Missing tool: npm"
}
Write-Ok "Required tools are available"

$secretFile = Join-Path $Root "deploy\k8s\secret.yaml"
if (-not (Test-Path -LiteralPath $secretFile)) {
    throw "deploy\k8s\secret.yaml is missing. Copy deploy\k8s\secret.example.yaml and set the Keycloak client secret."
}

Write-Step "Checking that Compose is not using Postgres, MongoDB, or Keycloak"
Assert-ComposeVolumesFree
Write-Ok "Shared volumes are free"

Write-Step "Applying manifests"
Push-Location $Root
try {
    kubectl apply -k deploy
    if ($LASTEXITCODE -ne 0) {
        throw "kubectl apply failed"
    }
}
finally {
    Pop-Location
}
Write-Ok "Manifests applied"

Write-Step "Waiting for pods"
$deadline = (Get-Date).AddSeconds(60)
do {
    $podLines = @(kubectl -n $Namespace get pods --no-headers 2>$null)
    if ($LASTEXITCODE -eq 0 -and $podLines.Count -gt 0 -and $podLines[0]) {
        break
    }
    Start-Sleep -Seconds 2
} while ((Get-Date) -lt $deadline)

kubectl -n $Namespace wait --for=condition=Ready pod --all --timeout="${ReadyTimeoutSec}s"
if ($LASTEXITCODE -ne 0) {
    throw "Pods did not become Ready within $ReadyTimeoutSec seconds"
}
Write-Ok "All pods are Ready"

Stop-TrackedApps | Out-Null

$started = @()
$forwards = @(
    @{ Name = "Keycloak"; Target = "svc/keycloak"; Ports = "8080:8080" },
    @{ Name = "Seq"; Target = "svc/seq"; Ports = "5341:80" },
    @{ Name = "AppointmentService"; Target = "svc/appointment-service"; Ports = "5067:8080" },
    @{ Name = "CommunicationService"; Target = "svc/communication-service"; Ports = "5080:8080" },
    @{ Name = "NotificationService"; Target = "svc/notification-service"; Ports = "5090:8080" }
)

foreach ($forward in $forwards) {
    Write-Step "Port-forward $($forward.Name)"
    $started += Start-AppWindow -Name $forward.Name -WorkingDirectory $Root -Command "kubectl -n $Namespace port-forward $($forward.Target) $($forward.Ports)"
}

if (-not $SkipSpa) {
    Write-Step "Starting SPA"
    $started += Start-AppWindow -Name "SPA" -WorkingDirectory $WebDir -Command "npm install; npm run dev"
}

Save-TrackedPids -Entries $started

Write-Host ""
Write-Host "Kubernetes:" -ForegroundColor Cyan
Write-Host "  Keycloak              http://localhost:8080"
Write-Host "  Seq                   http://localhost:5341"
Write-Host "  AppointmentService    http://localhost:5067"
Write-Host "  CommunicationService  http://localhost:5080"
Write-Host "  NotificationService   http://localhost:5090"
if (-not $SkipSpa) {
    Write-Host "  SPA                   http://localhost:3000"
}
Write-Host ""
Write-Host "Stop windows:  .\scripts\start-k8s.ps1 -Stop" -ForegroundColor DarkGray
Write-Host "Pods stay in the cluster until:  kubectl delete -k deploy" -ForegroundColor DarkGray
