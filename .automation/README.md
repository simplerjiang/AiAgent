# Multi-Agent Automation (Local)

This folder provides a lightweight, local workflow to run a plan -> develop -> test loop with
Git checkpoints, saved logs, and a rollback path. The scripts do not call AI directly; they
prepare the workspace and record state so Copilot can follow the prompts reliably.

## Layout
- tasks.json: Task queue and status
- state.json: Current run metadata (checkpoint tag, branch, log file)
- logs/: Run logs (ignored by git)
- prompts/: Role prompts for planner, developer, tester
- scripts/: PowerShell scripts for run, finalize, rollback

## Typical Flow
1) Start a run:
   - .\ .automation\scripts\run.ps1
2) Follow prompts:
   - .automation\prompts\planner.md
   - .automation\prompts\developer.md
   - .automation\prompts\tester.md
3) Finalize (runs tests by default and commits):
   - .\ .automation\scripts\finalize.ps1 -TaskId AUTO-001 -Message "auto: complete task"
4) Rollback if needed:
   - .\ .automation\scripts\rollback.ps1 -Force

## Playwright MCP (Edge)
Use Edge for UI validation with MCP Playwright. If the MCP server supports channel selection,
choose "msedge" and prefer a persistent context with your existing Edge profile.
See prompts/tester.md for the exact checklist.
