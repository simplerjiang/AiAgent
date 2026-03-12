# ISSUE-20260312 P0-R1 Planning Report (EN + ZH)

## EN
### Summary
Planned and promoted the requested "pure frontend visual replay / developer mode" into a dedicated P0 remaining task: `ISSUE-20260310-P0-R1`.

### Actions Completed
- Added `ISSUE-20260310-P0-R1` into `.automation/tasks.json`.
- Kept `ISSUE-20260310-P0` as completed.
- Updated `README.md` checklist to mark P0 done and added P0-R1 as pending.
- Updated `.automation/state.json` current run to point to P0-R1 planning.
- Added a new P0-R1 execution blueprint in `.automation/plan/ISSUE-20260310-P0-R1.md`.
- Added one new continuous rule in `.github/copilot-instructions.md` for future roadmap-to-scope promotions.

### Planned Scope (P0-R1)
- Frontend Developer Mode toggle and gated access.
- Governance dashboard for source health, candidate verification, change queue, rollback history.
- TraceId-centered visual replay and error snapshot view.
- Minimal read-only backend APIs supporting the dashboard.
- Unit-first then Edge MCP verification.

### Validation Commands
```powershell
Get-Content .automation/tasks.json | ConvertFrom-Json | Out-Null
Get-Content .automation/state.json | ConvertFrom-Json | Out-Null
foreach ($p in @('README.md','.automation/tasks.json','.automation/state.json','.automation/plan/ISSUE-20260310-P0-R1.md','.automation/reports/ISSUE-20260312-P0-R1-PLAN.md')) { Select-String -Path $p -Pattern 'ISSUE-20260310-P0-R1' -SimpleMatch }
```

### Validation Result
Passed.
- `tasks.json: OK`
- `state.json: OK`
- `ISSUE-20260310-P0-R1` reference found in all synced artifacts (README/tasks/state/plan/report).
- Note: `rg` is unavailable in current PowerShell session, so reference verification was executed via `Select-String`.

### Issues
None during planning update.

## ZH
### 摘要
已将你提出的“纯前端可视化复现/开发者模式”正式前置为 P0 剩余任务：`ISSUE-20260310-P0-R1`，并完成计划层面的全量同步。

### 已完成动作
- 在 `.automation/tasks.json` 新增 `ISSUE-20260310-P0-R1`。
- 保持 `ISSUE-20260310-P0` 为已完成状态。
- 在 `README.md` 中将 P0 勾选为已完成，并新增 P0-R1 待办项。
- 在 `.automation/state.json` 将当前运行上下文切换到 P0-R1 规划。
- 新增 `.automation/plan/ISSUE-20260310-P0-R1.md`，给出完整执行蓝图。
- 在 `.github/copilot-instructions.md` 新增一条可执行持续规则（后续计划前置时统一做 R1 任务化同步）。

### P0-R1 规划范围
- 前端 Developer Mode 开关与权限隔离。
- 来源健康、候选源验证、修复队列、回滚历史等治理仪表盘。
- 基于 `traceId` 的可视化复现与错误快照查看。
- 支撑仪表盘的最小后端只读查询接口。
- 验证顺序固定：先 Unit，再 Edge MCP。

### 校验命令
```powershell
Get-Content .automation/tasks.json | ConvertFrom-Json | Out-Null
Get-Content .automation/state.json | ConvertFrom-Json | Out-Null
foreach ($p in @('README.md','.automation/tasks.json','.automation/state.json','.automation/plan/ISSUE-20260310-P0-R1.md','.automation/reports/ISSUE-20260312-P0-R1-PLAN.md')) { Select-String -Path $p -Pattern 'ISSUE-20260310-P0-R1' -SimpleMatch }
```

### 校验结果
已通过。
- `tasks.json: OK`
- `state.json: OK`
- `ISSUE-20260310-P0-R1` 已在 README/tasks/state/plan/report 五处同步命中。
- 说明：当前 PowerShell 无 `rg` 命令，已使用 `Select-String` 完成等价检索校验。

### 问题
本次仅为计划升级，无阻塞问题。
