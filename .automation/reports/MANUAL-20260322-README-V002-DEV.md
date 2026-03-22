# MANUAL-20260322 README v0.0.2 DEV

## EN

### Actions

- Rewrote README.md around the new public-facing version label `v0.0.2`.
- Replaced the previous screenshot references with the user-provided Chinese screenshot assets under `docs/screenshots`.
- Updated the README structure to emphasize the latest completed capabilities: stock terminal, multi-agent analysis, market sentiment, local news archive, trading-plan workflow, Stock Copilot MCP runtime foundation, draft session contract, desktop packaging, and GitHub Releases updates.
- Added a clearer roadmap section split into near-term, mid-term, and long-term directions.

### Validation Commands And Results

- Command: `git diff --check -- README.md`
- Result: passed, no whitespace or patch-format issues.

### Issues

- The working tree already contained many unrelated tracked and untracked changes before this README task started, so the commit for this round must be scoped to README, screenshot assets, and this report only.

## ZH

### 本轮操作

- 以对外版本 `v0.0.2` 为主线重写了 `README.md`。
- 将原来的截图引用替换为用户手动放入 `docs/screenshots` 下的中文截图文件。
- 按“最新能力 -> 核心能力 -> 截图 -> 已实现 -> 未来规划 -> 安装使用”的顺序重组 README，让对外说明更贴近当前产品状态。
- 在 README 中补充了近期 / 中期 / 长期规划，明确 Stock Copilot 后续将从 MCP 基础层继续推进到正式产品层闭环。

### 验证命令与结果

- 命令：`git diff --check -- README.md`
- 结果：通过，未发现空白字符或 patch 格式问题。

### 问题说明

- 本轮开始前工作区已经存在大量与 README 无关的已修改和未跟踪文件，因此提交时需要严格限制范围，只提交 README、截图资源和本报告，避免误带其它开发中内容。