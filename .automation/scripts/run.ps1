param(
  [string]$TaskId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$AutomationRoot = Resolve-Path (Join-Path $ScriptRoot "..")
$RepoRoot = Resolve-Path (Join-Path $AutomationRoot "..")

$TasksPath = Join-Path $AutomationRoot "tasks.json"
$StatePath = Join-Path $AutomationRoot "state.json"
$LogDir = Join-Path $AutomationRoot "logs"

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
$Timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$LogPath = Join-Path $LogDir "run-$Timestamp.log"

function Write-Log {
  param([string]$Message)
  $line = "$(Get-Date -Format "yyyy-MM-dd HH:mm:ss") $Message"
  Add-Content -Path $LogPath -Value $line
  Write-Host $line
}

Write-Log "Automation run started."

$gitStatus = git -C $RepoRoot status --porcelain
if ($gitStatus) {
  Write-Log "ERROR: Git working tree is not clean. Commit or stash changes first."
  exit 1
}

if (-not (Test-Path $TasksPath)) {
  Write-Log "ERROR: tasks.json not found."
  exit 1
}

$tasks = Get-Content $TasksPath -Raw | ConvertFrom-Json
$task = $null

if ($TaskId) {
  $task = $tasks.tasks | Where-Object { $_.id -eq $TaskId } | Select-Object -First 1
} else {
  $task = $tasks.tasks | Where-Object { $_.status -eq "todo" } | Select-Object -First 1
}

if (-not $task) {
  Write-Log "No pending tasks found."
  exit 0
}

$task.status = "in_progress"
if ($task.stages) {
  $task.stages.plan = "todo"
  $task.stages.dev = "todo"
  $task.stages.test = "todo"
}

$tasks | ConvertTo-Json -Depth 8 | Set-Content -Path $TasksPath -Encoding UTF8

$checkpointTag = "auto-start-$Timestamp"
$branchName = "auto/$($task.id)/$Timestamp"

git -C $RepoRoot tag $checkpointTag | Out-Null
git -C $RepoRoot checkout -b $branchName | Out-Null

$state = @{
  currentRun = @{
    timestamp = $Timestamp
    taskId = $task.id
    checkpointTag = $checkpointTag
    branch = $branchName
    logPath = $LogPath
  }
  lastCompletedTask = $null
}

$state | ConvertTo-Json -Depth 6 | Set-Content -Path $StatePath -Encoding UTF8

Write-Log "Task: $($task.id) - $($task.title)"
Write-Log "Checkpoint tag: $checkpointTag"
Write-Log "Branch: $branchName"
Write-Log "Next: follow prompts in .automation/prompts (plan -> dev -> test)."
Write-Log "After test, run .automation/scripts/finalize.ps1 -TaskId $($task.id) -Message \"auto: complete task\""
