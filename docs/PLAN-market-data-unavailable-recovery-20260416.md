# 市场数据不可用恢复计划（2026-04-16）

## 0) 执行状态更新（2026-04-17）

### 0.1 已完成（代码落地）

- 后端三源切换已落地。
- 成交额链路已解耦，不再依赖 breadth 成功。
- audit reasons 已可读。

### 0.2 已完成（验证结果）

- `EastmoneySectorRotationClientTests` 定向通过（9/9）。
- smoke 最新日志显示 5 源全绿：`logs/bkzj-smoke-20260417-163457.json`。

### 0.3 待完成（最终放行门槛）

- 仍需完成盘中 3 轮验收（4/21-4/23），全部全绿后最终放行。

## 背景与目标
当用户看到以下提示时：
- 市场成交额暂未同步完成
- 行业板块排行暂未同步完成
- 概念板块排行暂未同步完成
- 风格板块排行暂未同步完成
- 板块排行暂未同步完成

本计划目标是：在不牺牲系统稳定性的前提下，先保证用户“有可用信息可看”（止血），再完成数据链路根因修复（根治），并建立可监控、可告警、可回滚的稳定性工程闭环。

---

## 1) 问题确认

### 1.1 用户文案与后端触发条件一一对应
| 用户可见文案 | Degraded Flag | 触发位置与条件（代码事实） |
|---|---|---|
| 市场成交额暂未同步完成 | `market_turnover_unavailable` | `SectorRotationIngestionService.BuildDegradedFlags(...)` 中 `totalTurnoverBase <= 0` 时触发 |
| 行业板块排行暂未同步完成 | `sector_rankings_industry_unavailable` | 对应 board fetch 失败，或该 board 返回 `count=0` 时触发 |
| 概念板块排行暂未同步完成 | `sector_rankings_concept_unavailable` | 对应 board fetch 失败，或该 board 返回 `count=0` 时触发 |
| 风格板块排行暂未同步完成 | `sector_rankings_style_unavailable` | 对应 board fetch 失败，或该 board 返回 `count=0` 时触发 |
| 板块排行暂未同步完成 | `sector_rankings_unavailable` | 三类 board（industry/concept/style）全部 `count=0` 时触发 |

### 1.2 关键事实链（现状）
| 事实 | 说明 |
|---|---|
| `totalTurnoverBase` 计算逻辑 | 优先 `breadthSnapshot.TotalTurnover`，否则回退 `allRows.Sum(TurnoverAmount)`，再否则 0 |
| `GetMarketBreadthFromPush2ExAsync` 限制 | 仅提供涨跌分布，不提供成交额；返回的 turnover 固定为 0 |
| 板块排行主数据源 | 目前主要依赖 `GetBoardRankingsAsync`（push2 接口） |
| 查询层降级可见性策略 | `SectorRotationQueryService` 在 degraded 且 sector snapshot 落后时，不再返回空，尽量展示最近有效榜单并附带降级标记 |

## 1.3 已验证的数据源实测证据（12轮）

> 以下结论基于脚本实测结果，不做乐观口径推断。

| 数据源与能力点 | 12轮结果 | 可用性结论 | 证据说明 |
|---|---|---|---|
| push2 板块排行 `industry/concept/style` | 12轮 0% 可用 | 不可用 | 三类 board 关键字段不可用，不能支撑板块排行输出 |
| push2 市场宽度 `clist(f6)` | 12轮 0% 可用 | 不可用 | 无法形成可用市场宽度结果 |
| push2ex `getTopicZDFenBu` | 12轮 100% 可用 | 部分可用 | 可返回涨跌分布，但无成交额、无板块排行字段 |
| push2ex `getTopicDTPool` | 12轮 100% 可用 | 部分可用 | 含 `amount`，但仅覆盖跌停池局部数据 |
| push2ex `getTopicZTPool` / `getTopicZBPool` | HTTP 可达但 `data` 常为 `null`，可用成功率 0% | 不可用 | 连通性存在但业务字段不可用 |
| 腾讯 `qt.gtimg.cn` | 12轮可连通 | 部分可用 | 字段不足：无板块排行、无市场总成交额、无分布桶 |

**结论（当前阶段）**
- 已确认 `totalTurnover` 存在稳定直出源：`eastmoney_market_fs_sh_sz`。
- 板块排行核心字段当前仍未恢复稳定来源，board probe v3 结果显示可达源 `field_valid_rate` 均为 0。
- limit 指标中 `limitUpCount`、`limitDownCount`、`brokenBoardCount` 可稳定获取；`maxStreak` 仍缺稳定来源。

### 1.5 字段补齐状态（v3 历史版本，见 1.8 获取最新）

> 数据依据：
> - `logs/total-turnover-probe-20260416-155556.json`（每源20轮）
> - `logs/board-probe-v3-20260416-161707.json`（14源、每源20轮）
> - `logs/limit-metrics-probe-v3-20260416-162858.json`（13源、每源20轮）

| 字段 | 是否稳定可得 | 来源 | 证据说明 |
|---|---|---|---|
| sectorCode | ✅ 是（待集成） | bkzj 双键合并（f12映射，v8探测） | 20/20 板块语义可用（BK编码），core5_construct_rate=20/20（全部 code 类型通过）。 |
| sectorName | ✅ 是（待集成） | bkzj 双键合并（f14映射，v8探测） | 与 sectorCode 同源同结论，f14 稳定返回板块名称。 |
| changePercent | ✅ 是（待集成） | bkzj 键=f3（v8探测） | key=f3 返回涨跌幅字段，双键合并含重试后 core5_construct_rate=20/20。 |
| mainNetInflow | ✅ 是（待集成） | bkzj 键=f62（v8探测） | key=f62 返回主力净流入，双键合并含重试后 core5_construct_rate=20/20。 |
| turnoverAmount_or_rank | ✅ 是（待集成，f62替代） | bkzj 键=f62（v8探测） | f62 同时作为排序替代字段；已与 mainNetInflow 共用，20/20 达标。 |
| totalTurnover | 是 | eastmoney_market_fs_sh_sz | `success_rate=1.0`、`payload_valid_rate=1.0`、`field_valid_of_success=1.0`（20/20），已验证为稳定直出源。 |
| limitUpCount | 是 | ths_limit_up_pool（主）/ em_datacenter_limit_pool（备） | `field_stability.limitUpCount.stable_source_found=true`，且主备均满足稳定门槛。 |
| limitDownCount | 是 | em_push2ex_dt_pool | `field_stability.limitDownCount.stable_source_found=true`，`field_presence_rate=1.0`。 |
| brokenBoardCount | 是 | ths_broken_pool | `field_stability.brokenBoardCount.stable_source_found=true`，`field_presence_rate=1.0`。 |
| maxStreak | ✅ 是（待集成） | ths_continuous_limit_up | `https://data.10jqka.com.cn/dataapi/limit_up/continuous_limit_up` 连续 20 轮字段存在率 20/20（100%）；`maxStreak = max(data[].height)`，盘后可用。 |

### 1.4 v2 实测补充（第2轮，每源20轮，14源探测）

> 执行时间：2026-04-16，脚本：`scripts/probe-sentiment-sources-v2.py`，结果日志：`logs/sentiment-source-probe-v2-20260416-153502.json`

| 结论项 | 说明 |
|---|---|
| 探测规模 | 14 个数据源，每源20轮，共 280 次探测 |
| 最优组合覆盖率 | **38.46%**（仅5/13个目标字段可覆盖），不是全字段恢复 |
| 缺失字段清单 | `sectorCode`、`sectorName`、`mainNetInflow`、`turnoverAmount`/排名替代字段、`limitUpCount`、`brokenBoardCount`、`maxStreak`、`totalTurnover` |
| ZTPool / ZBPool | HTTP 200 但 payload 持续无效（`data=null`），不得计入可用 |
| push2 关键接口 | 远端断开，板块排行与总成交额主链路不可用 |

**最新实测核心结论（截至 2026-04-16）**：`totalTurnover` 已恢复稳定直出，但板块排行核心字段与 `maxStreak` 仍未补齐，当前只能判定为“部分可用”，**禁止对外宣称已完全恢复**。
### 1.6 v5 已知接口模板定向探测结论（2026-04-16 18:07）

> 执行时间：2026-04-16，脚本：`scripts/probe-eastmoney-clist-board-known-patterns-v5.py`，结果日志：`logs/eastmoney-clist-board-known-patterns-v5-20260416-180753.json`

**定向探测规模与结果**：25 个已知接口模板组合，阶段A过滤 -> 阶段B入围 5 个候选源

**失败分层**：
| 失败模式 | 说明 |
|---|---|
| push2 clist 传输层 | 远端断开，不可联通 |
| datacenter 源 | success=0，schema/result 字段不匹配，无法形成有效结果 |
| nf boardfundflow | HTTP 200 但解析后 rows=0，无有效数据行 |

**核心数字**：
- 总组合数：25
- 阶段B入围：5
- 满足核心5字段稳定源：0

**明确结论**：本次定向探测确认，**板块排行核心字段（sectorCode/sectorName/changePercent/mainNetInflow/ranking）仍未恢复稳定来源**，候选源虽可联通但无法提供完整字段或数据有效率为 0。**禁止对外宣称板块排行已全面恢复**，当前仅能标记为"部分受限"。

### 1.7 v5.1 语义门禁复核结论（2026-04-16 19:xx，独立验证）

> 执行：Test Agent 独立复核，不依赖 v5.1 报告原文，直接读取日志和实时终端复测

**关键发现**：
- C017（push2ex-stockchanges）stageB 虽 20/20 成功、字段全，**但 code 全为个股 6 位数字（如 300804）**，不是板块编码，属于伪阳性。
- shortlist 中除 C017 外的 7 个组合实时复测：C018/C019/C020 连通但 board_semantic_rate=0/20；C007/C023/C024/C025 连接不可用（HTTP200 0/20）。
- 定义板块语义门禁：`code 匹配 ^(BK|HY|GN)，非6位纯数字，name 非空，单轮>=80% 通过` 后，shortlist 全部组合判定为未达标。

**结论**：v5.1 的"生产就绪"结论不成立，应撤销；board 核心字段在 push2 系接口上仍未找到可用源。

### 1.8 getbkzj 新源探测结论（2026-04-16，多轮独立验证）

> 执行：Test Agent 终端实测，来源 https://data.eastmoney.com/dataapi/bkzj/getbkzj

**新源发现过程**：从 `data.eastmoney.com/newstatic/js/bkzj/list.js` 反向提取到 `dataapi/bkzj/getbkzj` 接口，参数 key+code 组合独立探测。

**单键探测结果（board 语义）**：
| key | code | http200_rate | board_semantic_rate | 说明 |
|---|---|---|---|---|
| f62 | m:90+s:4 | 20/20 | 20/20 | 板块语义通过，字段仅含 f12/f14/f62（无 changePercent） |
| f62 | m:90+t:3 | 20/20 | 20/20 | 同上 |
| f62 | m:90+t:1 | 20/20 | 20/20 | 同上 |
| f3  | m:90+s:4 | 20/20 | 20/20 | 字段含 f12/f14/f3（changePercent 可用，无 mainNetInflow） |
| f3  | m:90+t:3 | 20/20 | 20/20 | 同上 |
| f3  | m:90+t:1 | 20/20 | 20/20 | 同上 |

**双键合并（f3+f62）含重试验证**：
- max_retries=2，retry_delay=0.2s，每 code 20 轮
- sectorCode（f12）: 20/20 三个 code 全通过
- sectorName（f14）: 20/20
- changePercent（f3）: 20/20（含重试后）
- mainNetInflow（f62）: 20/20
- rankOrTurnover（f62 复用）: 20/20
- **core5_construct_rate: 20/20 全部三类 code**

**字段映射**（实施时使用）：
- `f12` → `sectorCode`（板块代码，BK 开头）
- `f14` → `sectorName`（板块名称）
- `f3` → `changePercent`（今日涨跌幅）
- `f62` → `mainNetInflow` + `rankOrTurnover`（主力净流入，同时作为排序替代）

**code 分类对应**：
- `m:90+s:4` → 行业板块（industry）
- `m:90+t:3` → 概念板块（concept）
- `m:90+t:1` → 地域/风格板块（style/regional）

**实施参数建议**：
- max_retries=2，首发失败仅对失败键重试，不重试另一键
- 超过 2 次仍失败的轮次：标记该类型缺失，不用旧数据填充
- 成功率保障：含重试后三类 code 全部达 20/20（100%）

**结论**：`data.eastmoney.com/dataapi/bkzj/getbkzj` 双键合并方案已通过多轮独立验证，满足 core5 字段完整性阈值（>=0.95），**可作为板块排行主备替代源接入后端**。`maxStreak` 新源已完成稳定性验证，待后端集成。

### 1.9 maxStreak 新源验证结论（2026-04-16）

> 执行：Test Agent 终端实测（不落盘）

**结论**：已找到稳定来源 `ths_continuous_limit_up`：
- 接口：`https://data.10jqka.com.cn/dataapi/limit_up/continuous_limit_up?date={DATE}&page=1&limit=100`
- 提取规则：`maxStreak = max(data[].height)`（无数据则 0）
- 20 轮稳定性：HTTP 成功率 20/20，字段存在率 20/20（100%）
- 盘后可用：是（无需 token）

**淘汰来源**：
- `push2ex/getTopicZTPool`：`rc=102`（时段限制）
- `getTopicLianbanPool`：404
- datacenter `RPT_LIMIT_UP_POOL_*`：`code=9501`（报表不可用）

**实施建议**：后端 `GetMaxLimitUpStreakAsync` 优先切换到该 THS 接口，保留现有实现作为降级回退。
---

## 2) 根因分析（分层）

### 2.1 数据源不可达（网络/上游可用性）
- push2 端点在特定时段/网络条件下不可达时，板块排行与成交额相关能力都会受影响。
- 目前板块榜单强依赖单一来源（push2），单点失败会放大为多个降级文案同时出现。

### 2.2 数据源返回空（接口成功但无有效行）
- `BuildDegradedFlags(...)` 将“调用失败”和“返回 0 条”都视为不可用。
- 当三个 board 同时返回 0 条时，会触发 `sector_rankings_unavailable`，用户感知为“板块排行整体不可用”。

### 2.3 回退链路缺字段（成交额）
- push2 breadth 不可达时回退 push2ex breadth；该回退仅有涨跌分布，不含 turnover。
- 导致 `totalTurnoverBase` 可能最终为 0，进而触发 `market_turnover_unavailable`。

### 2.4 查询层可见性策略（体验侧）
- 查询层已采取“显示最近有效榜单”策略，减少空白页面。
- 但当前策略主要解决“有没有可看数据”，对“数据时效性透明度、降级分级提示、可操作建议”仍不足，用户仍会理解为“系统持续不可用”。

---

## 3) 解决方案（P0/P1/P2）

## 总体策略
先止血再根治：
- 当前阶段不存在“可完整替代”的单一可用源。
- P0 目标从“恢复完整数据”调整为“可信降级 + 可视化告知 + 严格阻塞口径”。
- P0（48小时）：先把用户可见层做成“可理解、可继续用、可判断风险”。
- P1（1周）：修复采集与计算主链路，降低降级触发频率。
- P2（持续）：建立稳定性工程与告警，避免同类问题重复发生。

### 现实约束说明（基于 v2 实测）

| 约束项 | 说明 |
|---|---|
| push2 主链路 | 当前网络条件下出口策略/白名单限制导致 push2 关键接口远端断开，板块分类字段与总成交额主链路**不可用** |
| ZTPool / ZBPool payload 无效 | HTTP 200 响应但 `data` 字段持续为 `null`，不能算作可用数据源 |
| 最优多源组合覆盖率 | 仅 38.46%，核心字段（sectorCode/sectorName/mainNetInflow/totalTurnover 等）无法通过现有可联通源补齐 |
| 禁止对外宣称已恢复 | 在上述核心字段缺失问题未解决前，**严禁**在界面提示、运营通知或技术报告中宣称"数据已恢复"，只能标注"部分受限" |

---

### 数据源恢复切回门槛（新增）
仅当以下条件同时满足，才可对外宣称“恢复”：
- 板块排行主源连续 10+ 轮可用率 >= 95%。
- `ZTPool`/`ZBPool` 的 `data` 非空率 >= 95%。
- 成交额来源完整性达标（可稳定形成市场总成交额，且字段口径通过审计校验）。

### P0：48小时内止血（用户可见层）
| 目标 | 动作 | 产出 |
|---|---|---|
| 降低“全不可用”体感 | 统一降级提示卡片：明确“受影响指标 + 最近有效快照时间 + 建议操作（切换维度/关注审计）” | 用户可读提示模板与前端展示规范 |
| 保证榜单可读性 | 对 `isDegraded=true` 且有历史榜单时，默认展示最近有效榜单并高亮“快照时间非最新” | 页面无空白、可继续浏览 |
| 强化透明度 | 在摘要区增加“本次同步状态”字段：完整/部分完成/关键缺失 | 用户知道是部分失败而非全部崩溃 |
| 运营可介入 | 提供简单 runbook：出现 5 条提示时，先看 `/api/market/audit` 与最新 snapshot 时间 | 值班处理指引 v1 |

### P1：1周内根因修复（采集与计算层）
| 目标 | 动作 | 产出 |
|---|---|---|
| 降低单源依赖风险 | 为板块排行增加第二数据源或镜像抓取策略（同字段口径映射） | 双源容灾设计与实现 |
| 修复成交额缺失 | 在 breadth 回退路径缺 turnover 时，补充可用成交额来源（例如 board rows 聚合优先级增强、可用时段缓存复用） | `totalTurnoverBase` 非零率提升 |
| 区分“失败”与“空集” | 在采集层记录 failure 与 empty 的不同原因码，避免同一文案掩盖不同故障 | 可定位的 degrade reason |
| 提升同步成功率 | 针对 push2 调用增加超时、重试、退避与熔断窗口策略 | 同步稳定性提升 |

### P2：稳定性工程（监控与告警）
| 目标 | 动作 | 产出 |
|---|---|---|
| 可观测性完整 | 将 `DataSourceTracker` 指标接入时序看板（成功率、连续失败、最近成功时间、延迟） | 市场数据健康仪表盘 |
| 告警可行动 | 设置分级告警：单 board 连续失败、三 board 全空、turnover 连续缺失、degraded 占比升高 | 值班告警策略 |
| 防回归 | 补齐自动化测试与故障演练脚本（source down / empty response / fallback no turnover） | 稳定性回归套件 |

---

## 4) 详细改动点（后端文件与改动方向）

> 说明：本节为实施建议，不是本次代码变更。

| 文件 | 建议改动方向 | 目的 |
|---|---|---|
| `backend/SimplerJiangAiAgent.Api/Modules/Market/Services/SectorRotationIngestionService.cs` | 细化 degraded flags（区分 fetch failed vs empty result）；补充 `totalTurnoverBase` 回退优先级策略；在 `RawJson.status` 中落更多诊断字段 | 让降级可解释、可定位 |
| `backend/SimplerJiangAiAgent.Api/Modules/Market/Services/EastmoneySectorRotationClient.cs` | 为 `GetBoardRankingsAsync` 与 breadth 采集增加更健壮的重试/超时/容错策略；为回退路径补充结构化诊断日志 | 提升采集成功率与排障效率 |
| `backend/SimplerJiangAiAgent.Api/Modules/Market/Services/SectorRotationQueryService.cs` | 继续强化 degraded + stale 场景展示策略：返回最近有效榜单同时附带“数据时效标签/可见性原因” | 避免前端误判“无数据” |
| `backend/SimplerJiangAiAgent.Api/Modules/Market/Services/DataSourceTracker.cs` | 扩展统计维度（按 boardType 成功率、empty rate、连续失败窗口） | 支撑监控告警阈值 |
| `backend/SimplerJiangAiAgent.Api/Modules/Market/MarketModule.cs` | 在 `/api/market/audit` 输出中增加关键降级聚合指标与近 N 次同步摘要 | 支撑运维与客服快速判断 |
| `backend/SimplerJiangAiAgent.Api/Infrastructure/Jobs/SectorRotationWorker.cs` | 针对盘中故障窗口增加短周期补偿同步策略（受控） | 缩短不可用持续时间 |
| `backend/SimplerJiangAiAgent.Api.Tests/SectorRotationQueryServiceTests.cs` | 增加 degraded/stale 可见性、reason 透传、空集与失败分离等测试用例 | 防回归 |
| `backend/SimplerJiangAiAgent.Api.Tests/SectorRotationIngestionServiceTests.cs` | 增加 turnover 缺失与 board 全空/部分空的组合测试 | 保证判定正确 |

---

## 5) 验收标准（可测试、可量化）

### 5.1 功能验收
| 编号 | 验收项 | 通过标准 |
|---|---|---|
| A1 | 五条用户文案映射准确 | 给定对应 degrade reason 时，前端文案 100% 命中且不串文案 |
| A2 | degraded + stale 可见性 | 当最新 market degraded 且 board 快照旧时，仍返回最近有效榜单，`isDegraded=true`，`degradeReason` 非空 |
| A3 | 成交额回退有效 | 在 breadth 无 turnover 场景下，`totalTurnoverBase` 能通过回退策略得到非零（若存在可用来源） |

### 5.2 稳定性验收
| 编号 | 指标 | 目标值 |
|---|---|---|
| S1 | 盘中 4 小时窗口内 sector 榜单全空占比 | < 5% |
| S2 | `market_turnover_unavailable` 触发率 | 较当前基线下降 >= 70% |
| S3 | 单次同步平均耗时 | 不高于现网基线 + 20% |
| S4 | 连续失败恢复时间（MTTR） | <= 30 分钟 |

### 5.3 测试验收
| 编号 | 测试项 | 标准 |
|---|---|---|
| T1 | 单元测试 | 新增/修改测试全部通过 |
| T2 | 集成验证 | 人工触发 `/api/market/sync` 后，`/api/market/audit` 可观察到正确 source 状态与降级原因 |
| T3 | 前端验收 | 用户可在降级时看到“最近有效榜单 + 降级原因 + 快照时间” |

### 5.4 字段级验收（新增）
| 验收对象 | 连通性要求 | 字段完整性要求 | 判定口径 |
|---|---|---|---|
| 板块排行（industry/concept/style） | HTTP 可达 | 三类排行字段完整可解析，且连续窗口可用率达标 | 可连通 != 可用；字段不完整即判定不可用 |
| 市场总成交额 | HTTP 可达 | 可稳定得到总成交额字段，且口径一致 | 缺字段或仅局部金额均不得判定为恢复 |
| 涨跌停池（`ZTPool`/`ZBPool`） | HTTP 可达 | `data` 非空率达门槛 | `data=null` 视为不可用 |
| 市场分布桶 | HTTP 可达 | 分布桶字段完整（而非仅总量） | 缺桶字段仅可标记部分可用 |

### 5.5 禁止宣称恢复（新增）
- 明确规则：可连通不等于可用。
- 若板块排行或成交额任一核心字段未达标，只能标记为“部分可用”，禁止宣称“已恢复”。

---

## 6) 风险与回滚方案

### 6.1 主要风险
| 风险 | 影响 | 缓解措施 |
|---|---|---|
| 新回退策略引入口径不一致 | 指标波动、用户误解 | 在响应中标记来源与置信度，保留审计字段 |
| 重试策略过激 | 上游限流、同步变慢 | 增加退避与熔断阈值，限制最大重试次数 |
| 可见性策略变化 | 用户对“旧数据”误读为“实时数据” | 显示快照时间与“非最新”提示 |
| 告警阈值不合理 | 告警风暴或漏报 | 先灰度阈值，按一周数据校准 |

### 6.2 回滚方案
| 场景 | 回滚动作 |
|---|---|
| 新采集策略导致错误率上升 | 开关回退到旧采集路径，仅保留现有 push2/push2ex 逻辑 |
| 新可见性策略引发前端误解 | 回滚到当前已验证展示逻辑（保留最近有效榜单但不新增额外提示） |
| 监控告警噪音过高 | 降级为观测模式，仅保留关键故障告警 |

---

## 7) 执行排期（按天）

| 天数 | 任务 | 负责人建议 | 输出物 |
|---|---|---|---|
| Day 1 | 复盘现状链路与故障样本，冻结 P0 文案和可见性方案 | 后端 + 前端 + 产品 | P0 方案评审记录 |
| Day 2 | 完成 P0 上线（降级提示、最近有效榜单、审计使用手册） | 前端 + 后端 | P0 发布版本 |
| Day 3 | 采集层改造设计（双源/镜像、失败与空集分离、重试与退避） | 后端 | P1 设计文档 |
| Day 4 | 实现采集与计算层核心改动，补充诊断字段 | 后端 | P1 代码与接口变更 |
| Day 5 | 补齐单测/集成测试，联调前端可见性 | 后端 + 前端 | 测试报告与联调记录 |
| Day 6 | 灰度发布，观察 `audit` 与降级率，调参 | 后端 + 运维 | 灰度观察日报 |
| Day 7 | 全量发布与复盘，沉淀 P2 监控告警与演练计划 | 全员 | 发布复盘与稳定性清单 |

---

## 8) 下一轮技术行动（48h）

优先级说明（最新）：
- P0-1：板块排行稳定源恢复（最高优先）
- P0-2：`maxStreak` 新源接入与回归验证
- P0-3：将 `totalTurnover` 新源接入并加监控

| 编号 | 行动项 | 说明 | 可交付产物 |
|---|---|---|---|
| N1 | 网络链路核查与复测 | 排查出口策略/白名单配置，确认 push2 关键接口是否被封堵；在出口策略变更后重新运行探测脚本进行复测 | 网络链路诊断报告 + 复测日志（JSON） |
| N2 | akshare 函数级验证（可联网依赖环境） | 在具备正常出口的环境中执行 akshare 板块/成交额相关函数，验证字段完整性与可用率 | akshare 函数级验证日志 + 字段覆盖率表格 |
| N3 | totalTurnover 直出源验证脚本 | 单独编写并执行验证脚本，探测能够直接返回市场总成交额字段的数据源（不依赖聚合计算），多轮验证稳定性 | `scripts/probe-total-turnover-sources.py` + 验证日志 |
| N4 | v3 探测脚本（纳入 N1/N2/N3 结论） | 将新可联通源与字段补全方案纳入探测矩阵，输出新一轮覆盖率报告 | `scripts/probe-sentiment-sources-v3.py` + 覆盖率对比报告 |
| N5 | v5.1 解析与传输分层复测 | 针对 v5 发现的 push2 clist 传输层故障、datacenter schema 不兼容、nf boardfundflow 解析无数据，执行专项复测：(1) utf-8-sig/BOM 编码修复，(2) schema 字段兼容性增强，(3) push2 传输层故障复现与诊断 | `scripts/probe-eastmoney-clist-board-known-patterns-v5.1.py` + 分层复测日志（已完成，结论：伪阳性，详见 1.7） |
| N6 | bkzj 新源后端集成 | 将 `data.eastmoney.com/dataapi/bkzj/getbkzj` 双键合并（f3+f62，max_retries=2）接入 `EastmoneySectorRotationClient.cs`，作为板块排行主备路径（push2 失败后切换），字段映射：f12→sectorCode, f14→sectorName, f3→changePercent, f62→mainNetInflow/rank | 后端代码改造 + 单元测试 + 集成验证（`/api/market/audit`） |

---

## 附：执行原则
1. 先止血再根治：先保障用户“看得到且看得懂”，再追求指标完整性。
2. 失败与空集分离：同样“不可用”外观，必须有可定位的内部原因。
3. 每个降级都有证据链：来源状态、快照时间、reason code、最近成功时间必须可追溯。
4. 所有策略可回滚：上线动作必须有开关与降级路径，避免扩大影响面。

---

## 实测产物引用
- `scripts/probe-sentiment-sources.ps1`
- `logs/sentiment-source-probe-20260416-150832.json`
- `scripts/probe-sentiment-sources-v2.py`
- `logs/sentiment-source-probe-v2-20260416-152923.json`
- `logs/sentiment-source-probe-v2-20260416-153502.json`
- `scripts/probe-total-turnover-sources.py`
- `logs/total-turnover-probe-20260416-155556.json`
- `scripts/probe-board-sources-v3.py`
- `logs/board-probe-v3-20260416-161707.json`
- `scripts/probe-limit-metrics-sources-v3.py`
- `logs/limit-metrics-probe-v3-20260416-162858.json`
- `scripts/probe-eastmoney-clist-board-known-patterns-v5.py`
- `logs/eastmoney-clist-board-known-patterns-v5-20260416-180753.json`
- `scripts/probe-eastmoney-clist-board-known-patterns-v5.1.py`
- `logs/eastmoney-clist-board-known-patterns-v5.1-20260416-184429.json`
- `logs/V5.1-EXECUTION-REPORT.md`（报告存在伪阳性，已由独立复核纠正，见 1.7）
- `[终端实测] getbkzj 双键合并 bkzj 探测 2026-04-16`（见 1.8，无单独日志文件，Test Agent 终端直跑）
