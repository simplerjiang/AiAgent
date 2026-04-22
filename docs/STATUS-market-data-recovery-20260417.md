# 市场数据恢复当前状态（2026-04-17）

## 背景

为确认市场数据链路从“不可用/降级”向“可用”恢复的真实进展，今日对市场同步与审计接口进行核验，并结合最新审计快照判断整体恢复等级。

本次结论基于以下事实：
- `/api/market/sync` 与 `/api/market/audit` 均可调用且返回 HTTP 200。
- 审计 `sources` 中 6 个关键源状态均为 `ok`。
- 但最新 `recentSync` 仍显示最终汇总结果未完成（降级态未退出）。

## 当前可用

1. 接口可用性恢复
- `/api/market/sync`：可调用，HTTP 200。
- `/api/market/audit`：可调用，HTTP 200。

2. 关键数据源状态恢复
- `bkzj_board_rankings`：`ok`
- `bkzj_board_rankings_industry`：`ok`
- `bkzj_board_rankings_concept`：`ok`
- `bkzj_board_rankings_style`：`ok`
- `ths_continuous_limit_up`：`ok`
- `eastmoney_market_fs_sh_sz`：`ok`

3. 成交额链路可用
- 最新 `recentSync` 中 `totalTurnover` 有值，说明成交额相关链路可工作。

## 当前不可用 / 未达标

1. 最终快照仍为降级态
- 最新 `recentSync.wasComplete=false`。
- 最新 `recentSync.sectorRowCount=0`。
- 说明即使关键源显示 `ok`，最终聚合结果仍未达到“完整可用”。

2. 退化标记未清除
- `degradedSources` / `reasons` 仍包含 `market_breadth_unavailable`。
- 仍包含 `sector_rankings_*` 相关退化项（板块排名链路的最终结果仍有缺口）。

3. 恢复等级判定
- 当前只能判定为“部分恢复”。
- 不满足“完全恢复”的放行条件。

## 需要修复（按优先级）

1. P0：`sources` 状态与 `recentSyncs` 结果不一致（观测语义问题）
- 问题：源状态均 `ok`，但最终 `wasComplete=false`、`sectorRowCount=0`。
- 风险：会导致“看起来恢复了但实际未恢复”的误判。
- 目标：统一观测语义，明确“源成功”与“最终快照成功”的判定边界，并在审计输出中可直接区分。

2. P0：board snapshot 最终结果为 0 行
- 问题：`sectorRowCount=0` 直接导致板块快照不可用。
- 风险：前端板块视图与策略依赖数据失真。
- 目标：修复板块快照落地链路，确保最终聚合非空并可持续产出。

3. P1：market breadth 持续 unavailable 根因
- 问题：`market_breadth_unavailable` 仍在退化列表。
- 风险：市场宽度指标长期缺失，影响健康度判断与信号稳定性。
- 目标：定位并修复宽度计算/输入依赖缺口，形成可回归验证的根因闭环。

4. P1：盘中 3 轮验收未完成（4/21-4/23）
- 问题：缺少连续盘中三轮（4/21、4/22、4/23）验收闭环结果。
- 风险：无法确认修复在真实交易时段的稳定性。
- 目标：按日内固定时点完成三轮验收并留存证据（接口、快照、退化项、关键字段）。

## 今日核验快照（接口与关键字段）

1. 接口层
- `/api/market/sync`：HTTP 200
- `/api/market/audit`：HTTP 200

2. sources（关键 6 项）
- `bkzj_board_rankings=ok`
- `bkzj_board_rankings_industry=ok`
- `bkzj_board_rankings_concept=ok`
- `bkzj_board_rankings_style=ok`
- `ths_continuous_limit_up=ok`
- `eastmoney_market_fs_sh_sz=ok`

3. latest recentSync
- `wasComplete=false`
- `sectorRowCount=0`
- `totalTurnover`：有值
- `degradedSources/reasons`：包含 `market_breadth_unavailable` 与 `sector_rankings_*` 相关项

## 放行标准与下一步

放行前最低标准：
1. 最新 `recentSync.wasComplete=true`。
2. `sectorRowCount>0` 且板块快照可被前端与下游消费稳定读取。
3. `degradedSources/reasons` 不再包含 `market_breadth_unavailable` 与 `sector_rankings_*` 退化项。
4. 完成盘中三轮连续验收（4/21-4/23）并有可追溯记录。

下一步执行建议：
1. 先修 P0 语义一致性与 board snapshot 0 行问题，再处理 breadth 根因。
2. 修复后立即进入盘中三轮验收，按统一模板输出日志与审计快照。
3. 在三轮全部通过前，仅维持灰度观察，不切换为“完全恢复”口径。

## 结论

- 可以继续灰度观察。
- 暂不建议宣称“完全恢复”。
