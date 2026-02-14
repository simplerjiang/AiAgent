param(
  [Parameter(Mandatory = $true)]
  [string]$TaskId,
  [string]$Message = "auto: complete task",
  [switch]$SkipTests
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$AutomationRoot = Resolve-Path (Join-Path $ScriptRoot "..")
$RepoRoot = Resolve-Path (Join-Path $AutomationRoot "..")

$TasksPath = Join-Path $AutomationRoot "tasks.json"
$StatePath = Join-Path $AutomationRoot "state.json"

if (-not $SkipTests) {
  dotnet test "D:\SimplerJiangAiAgent\backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj"
  Push-Location "D:\SimplerJiangAiAgent\frontend"
  npm run test:unit
  Pop-Location
}

$tasks = Get-Content $TasksPath -Raw | ConvertFrom-Json
$task = $tasks.tasks | Where-Object { $_.id -eq $TaskId } | Select-Object -First 1

if (-not $task) {
  throw "TaskId $TaskId not found in tasks.json."
}

$task.status = "done"
if ($task.stages) {
  $task.stages.plan = "done"
  $task.stages.dev = "done"
  $task.stages.test = "done"
}

$tasks | ConvertTo-Json -Depth 8 | Set-Content -Path $TasksPath -Encoding UTF8

$state = Get-Content $StatePath -Raw | ConvertFrom-Json
$state.lastCompletedTask = $TaskId
$state.currentRun = $null
$state | ConvertTo-Json -Depth 6 | Set-Content -Path $StatePath -Encoding UTF8

git -C $RepoRoot add -A

git -C $RepoRoot commit -m $Message
