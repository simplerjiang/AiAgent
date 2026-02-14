param(
  [string]$Tag,
  [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if (-not $Force) {
  Write-Host "Refusing to rollback without -Force."
  exit 1
}

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$AutomationRoot = Resolve-Path (Join-Path $ScriptRoot "..")
$RepoRoot = Resolve-Path (Join-Path $AutomationRoot "..")

$StatePath = Join-Path $AutomationRoot "state.json"

if (-not $Tag) {
  if (-not (Test-Path $StatePath)) {
    throw "state.json not found and no tag provided."
  }
  $state = Get-Content $StatePath -Raw | ConvertFrom-Json
  $Tag = $state.currentRun.checkpointTag
}

if (-not $Tag) {
  throw "No checkpoint tag available to rollback."
}

git -C $RepoRoot reset --hard $Tag
