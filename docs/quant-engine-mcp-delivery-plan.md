# 量化引擎 + MCP 交付计划（GOAL-AGENT-NEW-001 之后优先项）

## 1. 文档目的与执行原则
本文件用于在 GOAL-AGENT-NEW-001 完成后，立即启动量化引擎建设，并将其作为可控能力通过 MCP 提供给 LLM Agent 编排调用。目标是形成一套可执行、可验收、可灰度、可回滚的工程化交付方案。

执行原则：
- 先边界后实现：先明确 LLM 与量化引擎职责边界，再推进接口和策略实现。
- 先可用后增强：优先交付稳定规则与风控闸门，再引入复杂优化与 AI 解释层。
- 先观测后放量：全链路 trace、指标、回放能力先行，避免“黑箱策略”。
- 先小流量后全量：分阶段灰度，必须具备快速降级与回滚能力。

---

## 2. 总体目标与范围边界（P0/P1/P2）

### 2.1 总体目标
1. 建立可复现、可回测、可解释、可风控的量化决策引擎。
2. 通过 MCP 暴露标准化量化能力，供 LLM Agent 进行“解释、编排、问答、审计”。
3. 在保证风险约束下，实现研究效率提升、信号一致性提升和线上可用性提升。

### 2.2 范围边界

| 优先级 | 范围定义 | 包含内容 | 明确不包含 |
|---|---|---|---|
| P0 | 可运行最小闭环 | 因子计算、组合打分、风险闸门、MCP 查询与执行接口、基础回测、基础监控 | 高频撮合、复杂订单路由、跨市场套利执行 |
| P1 | 稳定增强 | 因子组合优化、事件流增量计算、策略配置中心、A/B 实验、可视化解释 | 全自动资金管理系统、复杂衍生品定价 |
| P2 | 智能化扩展 | 混合 AI 解释、策略建议生成、跨资产因子联动、自动参数巡检 | 无人工审查的全自动实盘全量放开 |

### 2.3 目标验收门槛（总体验收）
- 功能：关键路径接口成功率 >= 99.9%。
- 性能：核心打分接口 P95 延迟 <= 250ms（缓存命中场景），<= 900ms（冷算场景）。
- 质量：策略回测结果可复现（同参数同数据误差 < 0.1%）。
- 风险：超过阈值的交易建议必须被闸门阻断并可追溯。

---

## 3. 10+ 周详细路线图（12 周）

### 3.1 周度路线图总览

| 周次 | 阶段目标 | 关键任务 | 主要产出 | 验收标准 |
|---|---|---|---|---|
| W1 | 项目立项与边界冻结 | 需求冻结、数据字典、接口草案、风险清单 | PRD v1、架构草图、接口清单 v0 | 评审通过，需求变更控制生效 |
| W2 | 数据与特征基建 | 行情/资金/事件数据接入、清洗、标准化 | 数据接入任务、特征仓表结构 | 数据完整率 >= 99% |
| W3 | 因子引擎 MVP | 趋势/动量/反转/波动率因子实现 | 因子库 v1、单元测试 | 因子计算与样本基准一致 |
| W4 | 组合评分与风险闸门 | 多因子聚合、阈值、冷却、仓位约束 | 评分器 v1、风控规则 v1 | 风险违规建议阻断率 100% |
| W5 | 回测与评估框架 | 回测引擎、指标计算、参数快照 | 回测服务 v1、报告模板 | 回测可复现，指标口径一致 |
| W6 | MCP 第一版 | 查询/打分/解释接口、鉴权、幂等 | MCP API v1、OpenAPI 文档 | 契约测试通过率 100% |
| W7 | 前端展示与研究工作台 | 策略看板、信号解释、回测报告页 | 前端页面 v1、交互流程 | 关键流程可演示、无阻断缺陷 |
| W8 | 事件流与增量计算 | 事件驱动计算、缓存更新、回放通道 | 流式任务 v1、回放脚本 | 延迟目标达标，回放一致 |
| W9 | A/B 与灰度控制 | 实验分流、策略版本治理、降级开关 | 实验平台联调、开关矩阵 | 实验可观测、可回滚 |
| W10 | LLM Agent 编排接入 | 工具协议、Schema 校验、解释模板 | Agent Tool v1、提示模板 | Agent 调用成功率 >= 99% |
| W11 | 压测与故障演练 | 压测、熔断、超时、依赖降级 | 压测报告、演练报告 | SLO 达标，演练通过 |
| W12 | 试运行与上线评审 | 小流量试运行、复盘、放量决策 | 上线评审包、Runbook | 评审通过后进入全量计划 |

### 3.2 阶段里程碑

| 里程碑 | 时间 | 必交付 | 放行条件 |
|---|---|---|---|
| M1 需求冻结 | W1 末 | PRD、技术方案、风险目录 | 跨职能评审通过 |
| M2 MVP 闭环 | W4 末 | 因子+评分+风控闭环 | 回测与线上样例一致 |
| M3 MCP 可调用 | W6 末 | API 文档、权限、幂等 | 契约与安全测试通过 |
| M4 Agent 联动 | W10 末 | Agent 工具链、解释输出 | 误用率受控，闸门生效 |
| M5 上线决策 | W12 末 | 试运行报告、回滚预案 | 运营与技术共同签字 |

---

## 4. 具体开发任务流程（分角色/分域）

### 4.1 后端开发流程
1. 明确输入输出契约：定义因子请求、评分请求、风险评估请求结构。
2. 建立特征仓访问层：分离原始行情、衍生特征、策略配置。
3. 开发因子计算模块：保证纯函数化、可测试、可追溯。
4. 实现评分聚合器：支持加权、阈值、冲突处理、缺失值降级。
5. 风险闸门模块：仓位限制、最大回撤阈值、冷却期约束。
6. 集成 trace 与审计日志：每次评分附带 traceId、参数快照、版本号。
7. 输出 MCP 服务层：统一鉴权、限流、幂等、错误码。
8. 完成契约测试与回归测试。

后端流程产出：
- 可部署服务模块
- OpenAPI 文档
- 数据库变更脚本与回滚脚本
- 契约测试与性能基线报告

后端验收标准：
- 单测覆盖核心分支 >= 85%
- 关键接口错误码稳定，日志可追踪
- 风险规则命中行为符合预期

### 4.2 前端开发流程
1. 定义研究工作台信息架构：策略列表、信号详情、风险告警、回测报告。
2. 实现因子与组合可视化组件：趋势/动量/波动率分项解释。
3. 接入 MCP 调用链：请求状态、重试、降级展示。
4. 构建“建议-证据-风险”三段式 UI。
5. 增加审计视图：traceId 检索、版本比对、变更说明。
6. 完成可用性测试和异常场景 UI 验证。

前端验收标准：
- 关键路径交互成功率 >= 99%
- 无阻断级错误
- 降级态文案与可操作路径清晰

### 4.3 测试流程
1. 单元测试：因子、评分器、风险闸门、参数解析。
2. 集成测试：数据接入到评分输出端到端链路。
3. 契约测试：MCP schema、错误码、幂等语义。
4. 性能测试：并发与缓存命中/失配场景。
5. 故障注入：上游超时、坏数据、重复请求、权限异常。
6. 回归测试：版本升级后的指标与行为一致性。

测试验收标准：
- 核心链路 0 P0 缺陷
- 性能指标达标
- 所有降级路径可触发且可恢复

### 4.4 运维流程
1. 基础设施准备：环境变量、密钥、配置中心。
2. 部署策略：蓝绿/金丝雀配置。
3. 监控接入：业务指标、系统指标、链路指标。
4. 告警策略：错误率、延迟、风控阻断异常。
5. 故障演练：依赖降级、快速回滚、数据修复。
6. Runbook 固化：值班、升级、回溯流程。

运维验收标准：
- 关键告警 5 分钟内可触发
- 回滚流程可在 15 分钟内完成
- 日志检索与 trace 追踪可用

### 4.5 数据流程
1. 数据源接入评估：行情、资金流、事件新闻、行业指数。
2. 数据清洗与质量规则：缺失、重复、异常值处理。
3. 特征产出调度：批处理 + 增量计算。
4. 数据版本管理：时间戳、来源、加工链。
5. 数据回放能力：指定时间窗重放并比较。

数据验收标准：
- 数据时效符合 SLA
- 数据质量异常可告警
- 任意结果可追溯到源数据版本

### 4.6 产品验收流程
1. 验收场景库定义：上涨、下跌、震荡、黑天鹅场景。
2. 逐场景验收策略输出：是否可解释、是否可执行、是否可控。
3. 业务规则签字：风险阈值、禁用条件、人工覆盖条件。
4. 上线前复盘：问题闭环与改进项归档。

产品验收标准：
- 场景覆盖完整
- 风险认知一致
- 上线开关与回退规则明确

---

## 5. 技术实现路线对比（至少 4 种）

| 路线 | 核心思路 | 优势 | 劣势 | 适用阶段 | 推荐度 |
|---|---|---|---|---|---|
| 路线 A：纯规则引擎 | 预定义规则 + 阈值判定 | 可解释性强、上线快、风险可控 | 适应性弱、参数维护成本高 | P0 | 高 |
| 路线 B：因子引擎 + 优化器 | 多因子打分 + 参数优化 | 平衡效果与解释性，扩展性好 | 需要较好数据质量与回测体系 | P0-P1 | 很高 |
| 路线 C：事件驱动流式引擎 | 实时事件触发增量重算 | 时效好、适配快节奏市场 | 工程复杂度高、运维压力大 | P1 | 中高 |
| 路线 D：混合 AI 辅助解释 | 引擎决策 + LLM 解释编排 | 人机交互友好、研究效率高 | 存在幻觉与误读风险 | P1-P2 | 高（需防护） |
| 路线 E：端到端机器学习策略 | 直接预测收益/方向 | 潜在上限高 | 黑箱重、可解释性差、合规风险高 | P2 以后探索 | 低（当前不建议主线） |

结论：
- 主线建议采用“路线 B + 路线 D”，并保留路线 A 作为稳态兜底。
- 路线 C 在 P1 引入，用于提升时效，但必须在可观测能力完善后推进。

---

## 6. 多组合量化因子设计（8 个组合包）

### 6.1 组合因子设计表

| 组合包 | 信号逻辑 | 适用市场状态 | 主要风险点 | 参数建议 |
|---|---|---|---|---|
| 趋势组合包 | MA 多周期共振 + ADX 强度过滤 + 突破确认 | 单边上行/下行 | 假突破、滞后入场 | MA(20,60,120), ADX>22, 突破确认 2-3 根K |
| 动量组合包 | ROC + RSI 区间上穿 + 量价同步 | 趋势延续阶段 | 高位追涨回撤 | ROC 10-20, RSI 上穿 55/60, 成交量>1.2x |
| 反转组合包 | 极值偏离 + 布林带回归 + 成交量背离 | 超跌反弹/超涨回落 | 刀口抄底、连续单边 | Z-Score 阈值 2.0, 布林回归确认 1-2 根 |
| 波动率组合包 | ATR 扩张/收敛 + 波动率分位切换 | 震荡转趋势、趋势转震荡 | 波动骤升导致止损频繁 | ATR 周期 14, 波动分位 20/80 |
| 资金流组合包 | 主力净流入 + 大单占比 + 量比偏离 | 资金驱动行情 | 资金信号虚假、短期噪声 | 资金窗口 5/20 日, 大单占比阈值 60% |
| 事件情绪组合包 | 新闻情绪得分 + 事件等级 + 时间衰减 | 财报/政策/突发事件 | 消息失真、发布时间滞后 | 情绪阈值 0.65, 衰减半衰期 24h |
| 行业轮动组合包 | 行业强弱排序 + 相对收益 + 资金切换 | 结构性轮动市场 | 轮动过快、切换成本高 | 行业窗口 10/30 日, TopN=3-5 |
| 防御型组合包 | 低波红利 + 回撤约束 + 风险平价仓位 | 高不确定性与回撤期 | 收益上限受限 | 最大回撤阈值 8%-12%, 单标的仓位 <= 15% |

### 6.2 因子计算公式速查（核心因子）

| 因子 | 计算公式 | 预热期 | 输出范围 |
|---|---|---|---|
| MA_Cross_5_20 | signal = SMA(C,5) - SMA(C,20)，金叉 = signal 由负转正 | 20 bars | [-∞, +∞] |
| EMA_Slope_20 | slope = EMA(C,20)_t - EMA(C,20)_{t-1} | 21 bars | [-∞, +∞] |
| RSI_14 | RSI = 100 - 100/(1+RS)，RS = AvgGain14/AvgLoss14 | 14 bars | [0, 100] |
| ATR_14 | ATR = EMA(max(H-L, |H-Cprev|, |L-Cprev|), 14) | 15 bars | [0, +∞] |
| NetMainInflow_5D | Sum(MainBuy - MainSell, 5日) | 5 bars | [-∞, +∞] |
| NewsSent_72h | 带时间衰减加权情绪均值，半衰期 24h | 72h 窗口 | [-1, +1] |
| IndustryRank_20 | 行业内 20 日收益率截面排序百分位 | 20 bars | [0, 1] |
| Beta_60 | Cov(ri, rm) / Var(rm)，滚动 60 日 | 60 bars | [-∞, +∞] |

### 6.3 组合包协同建议
- 组合框架：核心仓位使用趋势+资金流，卫星仓位使用动量+轮动，防御仓位使用低波防御。
- 冲突规则：同一标的出现趋势多头与反转空头冲突时，优先风险闸门与中性减仓策略。
- 市场状态切换：通过波动率组合包控制其他组合包权重动态调整。

### 6.4 A 股特殊约束
- T+1：当日买入不可卖出，因子信号与执行窗口必须考虑 1 日延迟。
- 涨跌停：涨停/跌停标的不可成交，因子计算需排除或标记涨跌停状态。
- 停牌：停牌期间冻结该标的因子更新与仓位调整。
- 最小交易单位：100 股整手，仓位计算结果需向下取整。
- ST/*ST 标记：默认从 universe 排除，除非用户显式纳入。
- 集合竞价：9:15-9:25 与 14:57-15:00 数据需区别标记或过滤。

### 6.5 因子扩展性架构

> 核心原则：新增因子不应修改框架代码，只需实现接口 + 注册元数据即可自动参与计算、回测和评分。

#### 因子模块接口定义

```csharp
public interface IFactorModule
{
    /// 因子唯一标识（如 "MA_Cross_5_20"）
    string FactorKey { get; }
    
    /// 因子分类（Trend / Momentum / Reversal / Volatility / CashFlow / Sentiment / Rotation / Defensive）
    FactorCategory Category { get; }
    
    /// 最小预热 bar 数
    int WarmupBars { get; }
    
    /// 输出范围描述（如 "[0, 100]" 或 "[-∞, +∞]"）
    string OutputRangeDescription { get; }
    
    /// 计算因子值
    FactorResult Compute(FactorInput input);
}

public record FactorInput(
    IReadOnlyList<decimal> ClosePrices,
    IReadOnlyList<decimal> HighPrices,
    IReadOnlyList<decimal> LowPrices,
    IReadOnlyList<decimal> Volumes,
    IReadOnlyList<DataQualityFlag> QualityFlags,
    DateTimeOffset AsOf
);

public record FactorResult(
    string FactorKey,
    decimal Value,
    decimal Normalized,
    string Signal,      // strong_buy / bullish / ... / strong_sell
    string Quality      // ok / insufficient_data / limit_affected / no_data / partial_data
);
```

#### 因子注册与发现

- **自动发现**：所有实现 `IFactorModule` 的类通过 DI 容器自动注册，使用 `IServiceCollection.Scan()` 扫描 `Modules.Quant.Factors` 命名空间
- **因子注册表**：`FactorRegistry` 维护 `Dictionary<string, IFactorModule>`，提供按 FactorKey / Category / WarmupBars 查询
- **新增因子流程**：
  1. 实现 `IFactorModule` 接口
  2. 放入 `Modules/Quant/Factors/` 目录
  3. 自动被 DI 发现并注册到 `FactorRegistry`
  4. 在组合包配置 JSON 中添加权重即可参与评分
  5. 自动回测系统（§13 T51）自动纳入新因子的回测

#### 组合包配置热加载

组合包配置从 `QuantComboPackConfigs` 表读取 JSON 格式权重：
```json
{
  "packName": "trend_momentum_v1",
  "factors": {
    "MA_Cross_5_20": { "weight": 0.3, "required": true },
    "EMA_Slope_20": { "weight": 0.2, "required": false },
    "RSI_14": { "weight": 0.25, "required": true },
    "NetMainInflow_5D": { "weight": 0.25, "required": false }
  },
  "applicableMarketState": ["trending_up", "trending_down"],
  "version": "1.2.0"
}
```

修改配置后无需重启服务，`CompositeScorer` 通过缓存刷新自动加载新配置。

---

## 7. 量化 MCP 设计（端点分层、权限、幂等、trace、降级）

### 7.1 分层设计
- L0 能力层：因子计算、组合打分、风险评估、回测执行。
- L1 服务层：策略上下文组装、参数校验、统一错误模型。
- L2 MCP 接口层：对 Agent 暴露标准工具接口。
- L3 治理层：鉴权、限流、审计、配额、熔断、降级。

### 7.2 MCP 接口表示例

| 接口 | 方法 | 说明 | 权限 | 幂等键 | trace 字段 | 降级策略 |
|---|---|---|---|---|---|---|
| /mcp/quant/factors/evaluate | POST | 计算指定标的因子向量 | quant.read | X-Idempotency-Key | traceId, strategyVersion | 返回最近缓存结果并标记 stale |
| /mcp/quant/portfolio/score | POST | 输出组合评分与建议 | quant.score | X-Idempotency-Key | traceId, featureSnapshotId | 降级为规则引擎评分 |
| /mcp/quant/risk/check | POST | 风险闸门校验 | quant.risk | requestHash | traceId, ruleVersion | 超时时默认收紧风险（Fail-Close） |
| /mcp/quant/backtest/run | POST | 启动回测任务 | quant.backtest | jobId | traceId, datasetVersion | 回退到预计算报告 |
| /mcp/quant/report/get | GET | 获取解释报告 | quant.read | N/A | traceId, reportVersion | 返回上次成功报告 |
| /mcp/quant/explain/decision | POST | 输出可读解释（非改写决策） | quant.explain | requestHash | traceId, decisionId | 解释失败时返回结构化原始结果 |

### 7.3 权限模型
- 只读权限：仅可查询因子、评分、报告。
- 研究权限：允许触发回测与参数试验。
- 运营权限：允许灰度开关与策略版本切换。
- 管理权限：允许策略发布与风险阈值变更。

### 7.4 幂等与一致性
- 读写请求统一支持幂等键。
- 回测类任务采用 jobId 去重。
- 风险闸门默认 Fail-Close，避免误放开。
- 所有结果绑定版本号与数据快照号。

### 7.5 trace 与审计
- 每个调用必须附带 traceId。
- 审计记录包含：请求摘要、策略版本、参数摘要、风险命中、输出摘要。
- 错误链路保留外层与内层异常信息，便于定位上游故障。

### 7.6 降级策略
- 一级降级：复杂模型不可用时切换纯规则引擎。
- 二级降级：上游数据延迟时使用最近可信快照。
- 三级降级：风险评估异常时禁止交易建议，仅返回研究结论。

### 7.7 MCP 请求/响应示例

#### 示例 1：因子评估

```http
POST /api/mcp/quant/factors/evaluate
Content-Type: application/json
X-Idempotency-Key: f-eval-sh600519-20260328-1030

{
  "symbol": "sh600519",
  "asOf": "2026-03-28T10:30:00+08:00",
  "factors": ["MA_Cross_5_20", "RSI_14", "NetMainInflow_5D", "NewsSent_72h"],
  "frequency": "day"
}
```

```json
{
  "traceId": "qt-20260328-103000-001",
  "symbol": "sh600519",
  "asOf": "2026-03-28T10:30:00+08:00",
  "factors": [
    { "key": "MA_Cross_5_20", "value": 12.35, "normalized": 0.78, "signal": "bullish", "quality": "ok" },
    { "key": "RSI_14", "value": 62.1, "normalized": 0.55, "signal": "neutral", "quality": "ok" },
    { "key": "NetMainInflow_5D", "value": 3.42e8, "normalized": 0.81, "signal": "bullish", "quality": "ok" },
    { "key": "NewsSent_72h", "value": 0.35, "normalized": 0.62, "signal": "mildly_bullish", "quality": "ok" }
  ],
  "dataSnapshotId": "snap-20260328-1030",
  "computeMs": 47
}
```

#### 示例 2：风险闸门校验

```http
POST /api/mcp/quant/risk/check
Content-Type: application/json

{
  "symbol": "sh600519",
  "action": "buy",
  "positionPct": 0.12,
  "currentPortfolio": {
    "totalPositionPct": 0.75,
    "sameIndustryPct": 0.25,
    "todayTradeCount": 3
  }
}
```

```json
{
  "traceId": "qt-risk-20260328-103015",
  "passed": false,
  "violations": [
    { "rule": "industry_concentration", "limit": 0.20, "actual": 0.25, "action": "block" },
    { "rule": "daily_trade_limit", "limit": 5, "actual": 3, "action": "pass" }
  ],
  "suggestion": "行业集中度超限，建议将酿酒行业仓位降至 20% 以下后再操作"
}
```

#### 示例 3：组合打分

```http
POST /api/mcp/quant/portfolio/score
Content-Type: application/json

{
  "universe": ["sh600519", "sz000001", "sz300750"],
  "factorSet": "trend_momentum_v1",
  "asOf": "2026-03-28T10:30:00+08:00"
}
```

```json
{
  "traceId": "qt-score-20260328-103020",
  "scores": [
    {
      "symbol": "sh600519",
      "compositeScore": 1.84,
      "rank": 1,
      "signal": "strong_buy",
      "topFactors": ["NetMainInflow_5D", "MA_Cross_5_20"],
      "backtestMetrics": {
        "window60d": { "winRate": 0.65, "annualReturn": 0.23, "profitLossRatio": 2.1, "maxDrawdown": 0.08, "sharpeRatio": 1.45, "sampleCount": 18, "avgHoldingDays": 4.2 },
        "window120d": { "winRate": 0.58, "annualReturn": 0.18, "profitLossRatio": 1.8, "maxDrawdown": 0.12, "sharpeRatio": 1.12, "sampleCount": 35, "avgHoldingDays": 5.1 }
      }
    },
    {
      "symbol": "sz300750",
      "compositeScore": 0.42,
      "rank": 2,
      "signal": "hold",
      "topFactors": ["RSI_14"],
      "backtestMetrics": {
        "window60d": { "winRate": 0.52, "annualReturn": 0.05, "profitLossRatio": 1.1, "maxDrawdown": 0.15, "sharpeRatio": 0.35, "sampleCount": 12, "avgHoldingDays": 6.8 },
        "window120d": null
      }
    },
    {
      "symbol": "sz000001",
      "compositeScore": -0.31,
      "rank": 3,
      "signal": "weak_sell",
      "topFactors": ["EMA_Slope_20"],
      "backtestMetrics": null,
      "backtestStatus": "pending"
    }
  ],
  "strategyVersion": "trend_momentum_v1.2.0",
  "computeMs": 135
}
```

### 7.8 与现有 Research/MCP 系统的集成点

当前仓库已有完整的 Research Pipeline（ResearchRunner → ResearchRoleExecutor → ResearchSession/Turn）和 11 个 MCP 端点。量化引擎不替代它们，而是作为新的 MCP 能力层被 Research Pipeline 调用。

| 现有模块 | 集成方式 | 说明 |
|---|---|---|
| `ResearchRunner` | 新增 `QuantFactorStage` 阶段 | 在 Research 流程中插入因子评估作为 evidence 来源 |
| `ResearchRoleExecutor` | agent 工具列表新增 quant MCP tools | 让 analyst/commander 角色可调用因子评分 |
| `StockKlineMcp` / `StockMinuteMcp` | 数据层复用 | 量化引擎直接消费已有行情服务，不重复抓取 |
| `MarketContextMcp` | 市场状态输入 | 市场阶段判断作为组合包权重动态调整的输入 |
| `StockNewsMcp` | 事件情绪因子输入 | 新闻情绪组合包直接读取 LocalFact + AI 清洗结果 |
| `StockStrategyMcp` | 信号对齐 | 现有 TD/MACD 信号与量化因子信号做一致性校验 |
| `ActiveWatchlistService` | 默认 universe | 观察池作为量化引擎默认计算标的范围 |
| `TradingPlan` 模型 | 计划联动 | 因子评分变化可触发交易计划复核事件 |
| `HighFrequencyQuoteService` | 盘中增量数据源 | 分钟级因子更新消费实时行情推送 |
| `LlmService` | 解释层 | 因子结果由 LLM 组织为可读解释，不参与打分 |

---

## 8. 量化 MCP 提供给 LLM Agent 的可行性评估

### 8.1 能力边界（必须硬约束）
- LLM 负责：
  - 解释量化结果
  - 组织多步调用流程
  - 生成面向用户的可读摘要
- 量化引擎负责：
  - 因子计算与组合打分
  - 风险闸门与约束裁决
  - 可回放、可审计、可复现的确定性输出

边界原则：LLM 不直接生成交易分数，不绕过风险闸门，不改写引擎结论。

### 8.2 风险评估

| 风险类别 | 表现 | 影响 | 触发条件 |
|---|---|---|---|
| 幻觉风险 | LLM 编造不存在的因子或证据 | 错误解释、误导决策 | 上下文不完整或提示不严谨 |
| 误用风险 | 调用参数不合法、超权限调用 | 结果失真或安全事件 | 缺乏 schema 强校验 |
| 过度交易风险 | 高频触发导致频繁建议 | 成本上升、回撤扩大 | 缺乏冷却与节流机制 |
| 数据时效风险 | 使用过期数据生成结论 | 建议滞后 | 上游延迟或缓存污染 |
| 版本漂移风险 | 不同版本策略输出不一致 | 可复现性下降 | 灰度期间缺少版本绑定 |

### 8.3 防护设计
- Schema 防护：强类型输入输出，非法字段拒绝。
- 强校验：参数范围、市场时段、权限、配额、版本一致性全校验。
- 风险闸门：最大仓位、最大回撤、日内触发次数、黑名单资产。
- 冷却机制：同标的同方向建议触发冷却窗（例如 30-120 分钟）。
- 解释约束：LLM 只能引用引擎返回证据字段，不得外推为事实。
- 人工兜底：高风险场景要求人工确认后执行。

### 8.4 结论
结论：条件可行。
- 在“LLM 只解释与编排、引擎只做确定性裁决”的边界下，可安全推进。
- 必须先完成 schema、校验、风险闸门、冷却机制、审计追踪五件套，再扩大 Agent 权限。
- 不建议在无强风控和无审计链路条件下直接开放自动化执行。

---

## 9. 工程与容量评估

### 9.1 负载假设
- 标的规模：1000-3000。
- 更新频率：分钟级主链路 + 事件驱动补充。
- 并发调用：峰值 300-800 RPS（读多写少）。

### 9.2 容量规划表

| 维度 | 基线目标 | 峰值目标 | 备注 |
|---|---|---|---|
| 存储 | 500 GB 可用 | 2 TB 扩展 | 包含行情、特征、回测结果、审计日志 |
| CPU | 32 vCPU | 96 vCPU | 因子计算与回测并发需弹性扩容 |
| 内存 | 128 GB | 384 GB | 缓存热点特征与报告 |
| 延迟 | P95 <= 250ms（热） | P95 <= 900ms（冷） | score/risk 核心链路 |
| 可用性 | 99.9% | 99.95%（目标） | 依赖降级后保持核心可用 |

### 9.3 成本控制建议
- 读路径优先缓存与增量更新，减少重复计算。
- 回测任务异步化并采用队列隔离。
- 审计日志分级存储，热冷分层降低成本。

---

## 10. 质量指标体系

### 10.1 指标总览表

| 指标类别 | 指标项 | 目标值 | 观测周期 |
|---|---|---|---|
| 策略指标 | 年化收益、夏普、最大回撤、胜率、盈亏比、平均持仓天数、换手率 | 由策略组定义分层阈值 | 日/周/月 |
| 工程指标 | 接口成功率、P95 延迟、错误率、重试率 | 成功率 >= 99.9%，错误率 <= 0.2% | 分钟/小时 |
| 代理可用性指标 | Agent 调用成功率、schema 通过率、误用拦截率 | 成功率 >= 99%，schema 通过率 >= 99.5% | 日/周 |
| 风控指标 | 风险闸门触发率、违规放行率、冷却命中率 | 违规放行率 = 0 | 实时/日 |
| 数据指标 | 新鲜度、完整率、异常率 | 新鲜度 SLA 达标，完整率 >= 99% | 分钟/小时 |

### 10.2 验收口径
- 指标必须可追溯到原始日志与 trace。
- 同一指标口径在回测、仿真、线上保持一致定义。

---

## 11. 上线策略（灰度、A/B、回滚、应急）

### 11.1 灰度策略
- 第 1 阶段：内部研究账号 5%。
- 第 2 阶段：扩大到 20%，观察 3-5 个交易日。
- 第 3 阶段：50%，开启 A/B 与影子对照。
- 第 4 阶段：100% 放量（满足放行门槛后）。

### 11.2 A/B 设计
- A 组：规则引擎基线。
- B 组：因子引擎 + 优化器 + LLM 解释。
- 对比指标：收益风险比、信号稳定性、用户采纳率、误用率。

### 11.3 回滚策略
- 触发条件：错误率激增、风控异常、关键指标连续恶化。
- 回滚动作：
  1. 切换到规则引擎兜底版本。
  2. 禁用自动建议，仅保留研究输出。
  3. 保留 trace 与审计，进入故障复盘。

### 11.4 应急预案
- 数据故障：切换到最近可信快照，标记时效风险。
- 模型异常：停用优化器，启用固定参数模板。
- 接口拥塞：限流 + 队列削峰 + 优雅降级。


## 附录 C：量化引擎 + LLM 职责矩阵

> RAG 已决定暂不纳入，原因：投入产出比不足，LLM 本体能力已可覆盖解释与上下文组装需求。

| 场景 | 量化引擎负责 | LLM 负责 |
|---|---|---|
| "这只股票现在值不值得买？" | ✅ 因子打分 + 风控闸门 | ✅ 解释评分依据 |
| "最近有什么利好/利空？" | | ✅ 直接检索本地新闻 + MCP |
| "帮我回测这个策略" | ✅ 回测引擎 | ✅ 组织可读报告 |
| "上次为什么卖掉了？" | | ✅ 查询历史会话/计划 |
| "当前仓位风险怎样？" | ✅ 风险闸门 | ✅ 组织可读风险摘要 |
| "行业轮动到哪了？" | ✅ 轮动因子 | ✅ 结合新闻事件解释 |
| "帮我生成交易计划草稿" | ✅ 评分 + 约束 | ✅ 草稿编排与填充 |

核心原则：**量化引擎负责「算」，LLM 负责「说」。两者通过 MCP 解耦，不引入额外检索层。**

---

## 12. 风险清单与应对

| 风险ID | 风险描述 | 概率 | 影响 | 预警信号 | 应对策略 | 责任角色 |
|---|---|---|---|---|---|---|
| R1 | 数据源延迟/中断 | 中 | 高 | 数据新鲜度下降 | 快照降级 + 多源切换 | 数据工程 |
| R2 | 因子漂移导致失效 | 中 | 高 | 指标持续恶化 | 参数巡检 + 策略回退 | 量化研究 |
| R3 | LLM 幻觉解释 | 中 | 中 | 解释与证据不一致 | 证据白名单 + 强引用校验 | 平台工程 |
| R4 | 过度交易 | 中 | 高 | 日内触发次数异常 | 冷却机制 + 频控阈值 | 风控 |
| R5 | 接口性能抖动 | 中 | 中 | P95 上升 | 缓存优化 + 限流降级 | 后端/运维 |
| R6 | 权限误配 | 低 | 高 | 异常调用告警 | 最小权限 + 审计巡检 | 平台安全 |
| R7 | 发布回滚失败 | 低 | 高 | 回滚演练失败 | 预演 + 双版本并存 | 运维 |
| R8 | 指标口径不一致 | 中 | 中 | 报表冲突 | 指标字典统一 + 契约测试 | 产品/数据 |

---

## 13. 最终执行清单（可直接派工，45 条）

说明：
- 优先级：P0（立即）、P1（重要）、P2（优化）
- 依赖格式：Txx 表示依赖某任务先完成
- 命名空间前缀：后端 `SimplerJiangAiAgent.Api.Modules.Quant.*`，前端 `frontend/src/modules/quant/*`
- 所有因子公式参照 §6.2，组合包定义参照 §6.1，MCP 接口参照 §7.2，A 股约束参照 §6.4

---

### 基础建设阶段（T01 – T09：产品/数据基建）

#### T01 冻结量化引擎需求与边界文档
- **归属**：产品 | **优先级**：P0 | **依赖**：无
- **具体产出**：
  - PRD v1 文档（覆盖 §2.1 三大总体目标和 §2.2 P0/P1/P2 范围矩阵）
  - 需求边界矩阵（量化引擎 vs LLM 职责划分，对齐附录 C 职责矩阵）
  - 变更控制流程文档（需求冻结后的变更审批规则）
- **验收标准**：
  - 文档经跨职能评审并获得签字确认
  - 需求变更控制流程生效，任何范围变更需走审批
  - PRD 覆盖 §2.2 表中所有「包含内容」和「明确不包含」条目
- **技术要点**：
  - 边界必须与 §8.1 能力硬约束对齐：LLM 只解释与编排，引擎只做确定性裁决
  - 风险清单（§12）中 R1–R8 全部列入 PRD 风险章节
- **边界约束（不包含）**：
  - 具体算法选型与代码实现
  - 技术架构详细设计

#### T02 冻结指标字典与术语表
- **归属**：产品/数据 | **优先级**：P0 | **依赖**：T01
- **具体产出**：
  - 指标字典文档，覆盖 §6.2 全部 8 个核心因子定义（MA_Cross_5_20、EMA_Slope_20、RSI_14、ATR_14、NetMainInflow_5D、NewsSent_72h、IndustryRank_20、Beta_60）
  - 术语表文档（统一因子名、信号枚举值、市场状态标签）
  - 集合竞价数据消费策略文档：9:15-9:25 数据不参与因子计算（不可靠），14:57-15:00 数据标记 `AuctionPeriod` 后允许参与日频因子但不参与分钟频因子
- **验收标准**：
  - 字典完整覆盖 §6.2 的 8 个因子，每个因子包含：计算公式、预热期、输出范围、信号枚举
  - 术语表无歧义，经数据与后端团队共同签字
- **技术要点**：
  - 指标口径必须与 §10.2 验收口径一致：回测、仿真、线上使用同一定义
  - 信号枚举值统一为：`strong_buy`、`bullish`、`mildly_bullish`、`neutral`、`mildly_bearish`、`bearish`、`strong_sell`
- **边界约束（不包含）**：
  - 因子代码实现
  - P1/P2 扩展因子定义

#### T03 制定策略版本命名规范
- **归属**：产品/后端 | **优先级**：P0 | **依赖**：T01
- **具体产出**：
  - 版本命名规范文档（语义化版本 + 灰度标签，如 `trend_momentum_v1.2.0-canary`）
  - C# 值对象 `SimplerJiangAiAgent.Api.Modules.Quant.Models.StrategyVersion`
- **验收标准**：
  - 规范涵盖主版本 / 次版本 / 修订号 / 灰度标签四段式
  - `StrategyVersion` 类实现 `IComparable<StrategyVersion>` 和 `ToString()` 序列化
  - 单元测试覆盖版本比较、解析、序列化
- **技术要点**：
  - 版本号必须可嵌入 MCP 响应的 `strategyVersion` 字段（§7.7 示例）
  - 不可变值对象，重写 `Equals` / `GetHashCode`
- **边界约束（不包含）**：
  - 灰度发布基础设施实现
  - 版本自动升级机制

#### T04 完成行情数据源接入清单
- **归属**：数据 | **优先级**：P0 | **依赖**：T01
- **具体产出**：
  - 数据源映射表文档（行情 → K 线 / 分钟线 / 实时推送三路）
  - 复用验证报告：确认 `StockKlineMcp`、`StockMinuteMcp`、`HighFrequencyQuoteService`（§7.8）可直接被因子模块消费
  - 接口适配层 `SimplerJiangAiAgent.Api.Modules.Quant.Data.IMarketDataProvider`
- **验收标准**：
  - K 线日频、分钟频、实时行情三路数据均可通过 `IMarketDataProvider` 获取
  - 数据完整率 >= 99%（以最近 60 个交易日为验证窗口）
  - 接口调用耗时 P95 <= 50ms（本地缓存命中）
- **技术要点**：
  - 直接消费已有行情服务（§7.8），不重复抓取数据
  - A 股集合竞价时段（9:15–9:25、14:57–15:00）数据需区别标记（§6.4）
- **边界约束（不包含）**：
  - 新数据提供商接入
  - 港股 / 美股行情

#### T05 完成资金流数据源接入
- **归属**：数据 | **优先级**：P0 | **依赖**：T04
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Data.ICashFlowDataProvider` 接口与实现
  - SQLite 表 `QuantCashFlowDaily`（字段：Symbol、TradeDate、MainBuy、MainSell、LargeOrderRatio）
- **验收标准**：
  - 5 日窗口资金流数据完整率 >= 99%
  - `MainBuy`、`MainSell` 数值精度保留 2 位小数
  - `LargeOrderRatio` 正确计算（大单占比）
- **技术要点**：
  - 数据字段对齐 `NetMainInflow_5D` 因子所需的 `MainBuy` / `MainSell`（§6.2）
  - 大单占比阈值 60%（§6.1 资金流组合包参数建议）
- **边界约束（不包含）**：
  - 跨市场资金流联动分析
  - 实时盘中分笔资金流

#### T06 完成事件情绪数据源接入
- **归属**：数据 | **优先级**：P0 | **依赖**：T04
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Data.IEventSentimentDataProvider` 接口与实现
  - 复用验证报告：确认 `StockNewsMcp` + `LocalFact` AI 清洗结果（§7.8）可被 `NewsSent_72h` 因子消费
- **验收标准**：
  - 72h 窗口内新闻情绪数据可查，数据条目 >= 1 条/标的/天（活跃标的）
  - 情绪得分输出范围 [-1, +1]，精度 2 位小数
- **技术要点**：
  - 直接读取 `LocalFact` + AI 清洗结果（§7.8 `StockNewsMcp` 集成点）
  - 时间衰减半衰期 24h（§6.1 事件情绪组合包参数）
- **边界约束（不包含）**：
  - 自建 NLP 情绪分析模型
  - 社交媒体数据源

#### T07 建立特征仓表结构与索引
- **归属**：数据/后端 | **优先级**：P0 | **依赖**：T02, T04
- **具体产出**：
  - SQLite 表 `QuantFeatureStore`（字段：Id、Symbol、AsOf、FactorKey、Value、Normalized、Signal、Quality、DataSnapshotId、CreatedAt）
  - EF Core 实体 `SimplerJiangAiAgent.Api.Modules.Quant.Data.QuantFeatureEntity`
  - 复合索引：`IX_QuantFeatureStore_Symbol_AsOf_FactorKey`（唯一）
  - EF Core 迁移文件
- **验收标准**：
  - 按 `(Symbol, AsOf, FactorKey)` 查询耗时 <= 5ms（1000 条记录内）
  - 表结构支持存储 §6.2 全部 8 个因子的计算结果
  - 迁移文件可正确应用且回滚不报错
- **技术要点**：
  - 使用 SQLite + EF Core，与项目现有数据层一致
  - `Signal` 列使用字符串枚举（对齐 T02 术语表）
  - `DataSnapshotId` 用于版本绑定（§7.4）
- **边界约束（不包含）**：
  - 分布式存储方案
  - 时序数据库

#### T08 实现数据清洗与异常标注任务
- **归属**：数据 | **优先级**：P0 | **依赖**：T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Data.DataCleansingService`
  - 异常标注枚举 `DataQualityFlag`（Normal、LimitUp、LimitDown、Suspended、AuctionPeriod、StMarked）
- **验收标准**：
  - 涨跌停标的正确标注为 `LimitUp` / `LimitDown`（§6.4）
  - 停牌标的标注为 `Suspended`，集合竞价时段数据标注为 `AuctionPeriod`
  - ST / *ST 标的标注为 `StMarked`，默认从 universe 排除（§6.4）
  - 单元测试覆盖全部 6 种标注场景
- **技术要点**：
  - A 股特殊约束全覆盖（§6.4）：T+1、涨跌停、停牌、100 股整手、ST 排除、集合竞价过滤
  - 清洗规则为确定性逻辑，不使用 LLM
- **边界约束（不包含）**：
  - 复杂统计异常检测（如 3σ 离群值）
  - 历史数据回补
  - 数据过滤或排除 — T08 只做标注（DataQualityFlag），不做排除。下游消费者（T10-T17 因子模块、T18 评分器、T19 风险闸门）必须自行根据标注决定处理策略

#### T09 建立数据质量监控与告警
- **归属**：数据/运维 | **优先级**：P0 | **依赖**：T08
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Data.DataQualityMonitorService`（后台定时任务）
  - 监控指标：数据完整率、数据新鲜度、异常标注比例
  - 日志告警输出（结构化日志 + 控制台告警）
- **验收标准**：
  - 数据完整率 < 99% 时 5 分钟内输出告警日志
  - 数据新鲜度超过 SLA（15 分钟无更新）时触发告警
  - 监控服务崩溃可自动恢复
- **技术要点**：
  - ASP.NET Core `BackgroundService` 实现定时巡检
  - 告警输出到结构化日志，由运维层对接外部通知
- **边界约束（不包含）**：
  - 外部告警系统集成（钉钉 / 邮件 / PagerDuty）
  - 数据自动修复

---

### 因子引擎阶段（T10 – T17：因子模块）

> 因子模块统一接口：`SimplerJiangAiAgent.Api.Modules.Quant.Factors.IFactorModule`（完整定义见 §6.5）
> 公式参照 §6.2 因子计算公式速查表。每个因子模块为纯函数实现，输入 `FactorInput`，输出 `FactorResult`。
> 因子通过 DI 自动发现注册到 `FactorRegistry`（§6.5），新增因子只需实现接口 + 放入目录即可自动参与计算、回测和评分。
>
> 涨跌停处理策略：每个因子模块输入除行情序列外，还接收 `IReadOnlyList<DataQualityFlag> qualityFlags`（来自 T08）。涨停日（`LimitUp`）和跌停日（`LimitDown`）的收盘价为非自由价格，各因子需按如下策略处理：
> - 趋势/动量因子（MA、EMA、RSI）：涨跌停 bar 正常参与计算但在输出中标注 `quality = "limit_affected"`
> - 反转因子（ZScore、Bollinger）：涨跌停 bar 剔除出标准差计算窗口
> - 波动率因子（ATR）：涨跌停 bar 的 TrueRange 正常计算但标注
> - 资金流/情绪因子：不受涨跌停影响

#### T10 实现趋势因子模块
- **归属**：后端 | **优先级**：P0 | **依赖**：T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.TrendFactorModule : IFactorModule`
  - 因子实现：`MA_Cross_5_20`（SMA(C,5) − SMA(C,20)，金叉 = signal 由负转正）
  - 因子实现：`EMA_Slope_20`（EMA(C,20)_t − EMA(C,20)_{t−1}）
- **验收标准**：
  - `MA_Cross_5_20` 金叉/死叉信号与 §6.2 公式精确一致（浮点误差 < 1e-6）
  - `EMA_Slope_20` 斜率方向与趋势方向一致
  - 预热期 20 bars（MA_Cross）/ 21 bars（EMA_Slope），不足预热期返回 `quality = "insufficient_data"`
  - 单元测试分支覆盖 >= 85%
- **技术要点**：
  - 纯函数：输入 `IReadOnlyList<decimal> closePrices`，输出 `FactorResult`
  - SMA 使用滑动窗口避免重复计算
  - 金叉/死叉检测需比较当前 bar 与前一 bar 的 signal 值差
- **边界约束（不包含）**：
  - ADX 强度过滤（属趋势组合包编排层 T18）
  - 突破确认逻辑（属组合包）

#### T11 实现动量因子模块
- **归属**：后端 | **优先级**：P0 | **依赖**：T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.MomentumFactorModule : IFactorModule`
  - 因子实现：`RSI_14`（RSI = 100 − 100/(1+RS)，RS = AvgGain14/AvgLoss14）
- **验收标准**：
  - `RSI_14` 输出范围严格在 [0, 100]，与 §6.2 公式精确一致
  - AvgLoss = 0 时 RSI = 100，AvgGain = 0 时 RSI = 0
  - 预热期 14 bars，不足时返回 `quality = "insufficient_data"`
  - 单元测试分支覆盖 >= 85%
- **技术要点**：
  - Wilder 平滑法计算 AvgGain / AvgLoss（指数加权而非简单均值）
  - 除零保护：AvgLoss = 0 → RSI = 100
- **边界约束（不包含）**：
  - ROC（Rate of Change）因子（P1 动量组合包扩展）
  - 量价同步验证（属组合包编排层）

#### T12 实现反转因子模块
- **归属**：后端 | **优先级**：P0 | **依赖**：T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.ReversalFactorModule : IFactorModule`
  - 因子实现：`ZScore_20`（20 日收益率 Z-Score 偏离度）
  - 因子实现：`BollingerRevert_20`（布林带回归信号）
- **验收标准**：
  - Z-Score 阈值 2.0 时正确触发超买/超卖信号（§6.1 反转组合包参数）
  - 布林带回归信号在价格从带外回到带内时触发
  - 预热期 20 bars，单元测试分支覆盖 >= 85%
- **技术要点**：
  - Z-Score = (当日收益率 − μ_20) / σ_20
  - 布林带：中轨 = SMA(C,20)，上轨/下轨 = 中轨 ± 2σ
  - 回归信号需连续 1–2 根 K 线确认（§6.1）
- **边界约束（不包含）**：
  - 成交量背离检测（属组合包编排层）
  - 复杂统计套利模型

#### T13 实现波动率因子模块
- **归属**：后端 | **优先级**：P0 | **依赖**：T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.VolatilityFactorModule : IFactorModule`
  - 因子实现：`ATR_14`（ATR = EMA(TrueRange, 14)）
  - 因子实现：`VolatilityPercentile_60`（60 日波动率分位数）
- **验收标准**：
  - `ATR_14` = EMA(max(H−L, |H−Cprev|, |L−Cprev|), 14)，与 §6.2 公式精确一致
  - 预热期 15 bars（ATR）/ 60 bars（分位数），输出 [0, +∞]
  - 波动率分位切换点 20/80 正确触发信号（§6.1 波动率组合包参数）
  - 单元测试分支覆盖 >= 85%
- **技术要点**：
  - TrueRange = max(H−L, |H−Cprev|, |L−Cprev|)，需处理首根 K 线无 Cprev 的边界
  - 分位数使用滚动 60 日窗口排序
- **边界约束（不包含）**：
  - 隐含波动率（IV）
  - GARCH 模型

#### T14 实现资金流因子模块
- **归属**：后端 | **优先级**：P0 | **依赖**：T05, T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.CashFlowFactorModule : IFactorModule`
  - 因子实现：`NetMainInflow_5D`（Sum(MainBuy − MainSell, 5 日)）
- **验收标准**：
  - `NetMainInflow_5D` 计算与 §6.2 公式精确一致
  - 预热期 5 bars，输出 [-∞, +∞]
  - 缺失交易日数据时返回 `quality = "partial_data"` 并标注缺失天数
  - 单元测试分支覆盖 >= 85%
- **技术要点**：
  - 数据来源为 T05 的 `ICashFlowDataProvider`
  - 归一化使用截面百分位（跨标的排序）
- **边界约束（不包含）**：
  - 跨市场资金流联动分析
  - 分钟级资金流因子

#### T15 实现事件情绪因子模块
- **归属**：后端 | **优先级**：P0 | **依赖**：T06, T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.EventSentimentFactorModule : IFactorModule`
  - 因子实现：`NewsSent_72h`（带时间衰减加权情绪均值，半衰期 24h）
- **验收标准**：
  - `NewsSent_72h` 输出范围 [-1, +1]，与 §6.2 公式一致
  - 时间衰减函数：weight = exp(−λ × Δt)，λ = ln(2)/24h
  - 72h 窗口内无新闻时返回 `quality = "no_data"`，值为 0（中性）
  - 单元测试分支覆盖 >= 85%
- **技术要点**：
  - 数据来源为 T06 的 `IEventSentimentDataProvider`（复用 LocalFact AI 清洗结果，§7.8）
  - 情绪阈值 0.65 触发显著信号（§6.1 事件情绪组合包参数）
- **边界约束（不包含）**：
  - 自建 NLP 情绪分析模型
  - 实时新闻流式计算

#### T16 实现行业轮动因子模块
- **归属**：后端 | **优先级**：P1 | **依赖**：T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.IndustryRotationFactorModule : IFactorModule`
  - 因子实现：`IndustryRank_20`（行业内 20 日收益率截面排序百分位）
- **验收标准**：
  - `IndustryRank_20` 输出范围 [0, 1]，与 §6.2 公式一致
  - 预热期 20 bars，行业内标的数 < 5 时返回 `quality = "insufficient_universe"`
  - 单元测试分支覆盖 >= 85%
- **技术要点**：
  - 截面排序：同行业标的 20 日收益率排序后计算百分位
  - 行业分类复用 `MarketContextMcp`（§7.8）提供的行业信息
  - 行业窗口 10/30 日、TopN = 3–5（§6.1 行业轮动组合包参数）
- **边界约束（不包含）**：
  - 跨市场行业联动
  - 行业分类标准维护（使用已有分类）

#### T17 实现防御型风险因子模块
- **归属**：后端 | **优先级**：P1 | **依赖**：T07
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Factors.DefensiveFactorModule : IFactorModule`
  - 因子实现：`Beta_60`（Cov(ri, rm) / Var(rm)，滚动 60 日）
- **验收标准**：
  - `Beta_60` 与 §6.2 公式一致，滚动 60 日窗口
  - 预热期 60 bars，市场基准不可用时返回 `quality = "no_benchmark"`
  - Beta < 0.8 标记为低波防御型，Beta > 1.2 标记为高波进攻型
  - 单元测试分支覆盖 >= 85%
- **技术要点**：
  - 市场基准 rm 使用沪深 300 指数日收益率
  - 滚动协方差与方差使用在线算法减少内存占用
- **边界约束（不包含）**：
  - 风险平价仓位算法（属 T18 评分聚合层）
  - 最大回撤阈值设定（属 T19 风险闸门）

---

### 评分与风控阶段（T18 – T23：评分/风控/回测）

#### T18 开发组合评分聚合器 v1
- **归属**：后端 | **优先级**：P0 | **依赖**：T10, T11, T12, T13, T14, T15
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Scoring.CompositeScorer : ICompositeScorer`
  - 6 个 P0 组合包配置（趋势、动量、反转、波动率、资金流、事件情绪）
  - 2 个 P1 组合包 stub 配置（行业轮动、防御型）— 预留接口，因子就绪后启用；T16/T17 完成后启用对应组合包
  - 配置实体 `SimplerJiangAiAgent.Api.Modules.Quant.Scoring.ComboPackConfig`
  - SQLite 表 `QuantComboPackConfigs`（字段：PackName、FactorWeights JSON、ApplicableMarketState、Version）
- **验收标准**：
  - 加权聚合输出 `compositeScore`、`rank`、`signal`、`topFactors`（对齐 §7.7 示例 3 响应结构）
  - 缺失因子自动降级：可用因子不足 50% 时输出 `signal = "insufficient_data"`
  - 冲突处理：同一标的趋势多头与反转空头冲突时走中性减仓（§6.3）
  - 核心打分 P95 延迟 <= 250ms（缓存命中），<= 900ms（冷算）（§2.3）
  - 单元测试覆盖全部 6 个 P0 组合包
  - 停牌标的自动从评分 universe 排除或标记为 frozen
  - 市场状态输入可通过 MarketContextMcp 获取并影响组合包权重
- **技术要点**：
  - 策略版本绑定（T03 `StrategyVersion`），每次评分记录版本号
  - 市场状态切换通过波动率组合包动态调整其他包权重（§6.3）
  - 缓存层：内存缓存热点标的评分，TTL 与行情更新频率对齐
  - 消费 `MarketContextMcp`（§7.8 集成点 4）获取当前市场状态，动态调整组合包权重
  - 评分前过滤 universe：排除停牌标的、ST 标的（除非用户显式纳入）
  - 通过 `IUniverseProvider` 适配 `ActiveWatchlistService`（§7.8 集成点 7）作为默认评分 universe
- **边界约束（不包含）**：
  - 机器学习优化器（路线 B 的参数优化部分，P1）
  - A/B 实验分流逻辑（T44）
  - pass/block 交易决策判定（属 T19 风险闸门职责）— T18 只输出 score + signal，不做拦截判定

#### T19 开发风险闸门（仓位/回撤/频控）
- **归属**：后端/风控 | **优先级**：P0 | **依赖**：T18
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Risk.RiskGateService : IRiskGate`
  - 规则集：单标的最大仓位、行业集中度上限、日内触发次数上限、最大回撤阈值
  - 规则配置实体 `RiskRuleConfig`
  - SQLite 表 `QuantRiskRules`（字段：RuleName、Limit、Action、Enabled）
- **验收标准**：
  - 超阈值建议 100% 阻断（§2.3 风险闸门验收门槛）
  - 超时时默认 Fail-Close（§7.4），即收紧风险而非放行
  - 输出 `violations` 数组 + `suggestion` 文本（对齐 §7.7 示例 2 响应结构）
  - 单标的仓位 <= 15%、行业集中度 <= 20%、回撤阈值 8%–12%（§6.1 防御型组合包参数）
  - 仓位计算向下取整至 100 股整手（§6.4）
- **技术要点**：
  - 规则引擎使用策略模式，每条规则实现 `IRiskRule` 接口
  - A 股约束硬编码入规则：T+1（当日买入不可卖出）、涨跌停不可成交、停牌冻结（§6.4）
  - 规则版本绑定 `ruleVersion`（§7.2 trace 字段）
  - T19 的输入是 T18 的 compositeScore + signal，输出是 pass/block + violations。评分逻辑在 T18，拦截逻辑在 T19，职责严格分离
- **边界约束（不包含）**：
  - 动态阈值自适应（P2）
  - 跨账户风控聚合

#### T20 开发冷却机制与重复建议抑制
- **归属**：后端 | **优先级**：P0 | **依赖**：T19
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Risk.CooldownService`
  - 冷却窗口配置：同标的同方向 30–120 分钟（§8.3）
  - SQLite 表 `QuantCooldownRecords`（字段：Symbol、Direction、TriggeredAt、ExpiresAt）
  - 冷却状态查询方法 `CooldownService.GetActiveCooldowns(string symbol)`
- **验收标准**：
  - 冷却窗口内重复建议抑制率 100%
  - 冷却过期后可正常触发新建议
  - T+1 约束（§6.4）：买入建议冷却跨天自动清除
  - 单元测试覆盖：窗口内抑制、窗口过期放行、跨天清除
- **技术要点**：
  - 内存缓存（`ConcurrentDictionary`）+ SQLite 持久化双层，重启后从 DB 恢复
  - 冷却窗口可配置，默认同标的同方向 60 分钟
- **边界约束（不包含）**：
  - 用户级个性化冷却参数
  - 冷却窗口动态调整

#### T21 实现回测引擎最小闭环
- **归属**：后端 | **优先级**：P0 | **依赖**：T18
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Backtest.BacktestEngine : IBacktestEngine`
  - 支持：单策略、单标的、日频回测
  - 回测上下文 `BacktestContext`（参数快照、数据版本、时间窗口）
  - 回测结果 DTO `BacktestResult`（字段：`DailyPnl` 数组、`Trades` 数组、`FinalEquity`、`Metadata`）
- **验收标准**：
  - 同参数同数据回测误差 < 0.1%（§2.3）
  - 参数快照绑定 `datasetVersion`（§7.2 trace 字段）
  - T+1 约束：买入信号次日才可卖出（§6.4）
  - 涨跌停不可成交：涨停日买入跳过、跌停日卖出跳过（§6.4）
  - 单元测试覆盖：正常交易、T+1 延迟、涨跌停跳过、停牌冻结
  - BacktestResult 结构包含 dailyPnl 序列 + trades 列表，可被 T22 BacktestMetricsService 直接消费
- **技术要点**：
  - 时间序列回放：逐日遍历，每日调用 `CompositeScorer` + `RiskGateService`
  - 不使用未来数据（look-ahead bias 防护）：因子计算只使用 asOf 及之前的数据
  - 仓位计算向下取整至 100 股整手（§6.4）
- **边界约束（不包含）**：
  - 多策略组合回测
  - 分钟级回测
  - 滑点与交易成本精确模拟

#### T22 实现回测指标计算服务
- **归属**：后端 | **优先级**：P0 | **依赖**：T21
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Backtest.BacktestMetricsService`
  - 计算指标：年化收益率、夏普比率、最大回撤、胜率、盈亏比、平均持仓天数、换手率（§10.1 策略指标）
  - 输出 DTO `BacktestMetrics`（含 winRate、profitLossRatio、avgHoldingDays）
  - SQLite 表 `QuantStrategyPerformance`（字段：Id、StrategyVersion、ComboPackName、Symbol、BacktestWindow、WinRate、AnnualReturn、ProfitLossRatio、MaxDrawdown、SharpeRatio、SampleCount、ComputedAt）
  - EF Core 实体 `QuantStrategyPerformanceEntity`
- **验收标准**：
  - 指标口径与 §10.1 / §10.2 一致，回测与线上使用同一计算逻辑
  - 年化收益精度 2 位小数，夏普精度 2 位小数，最大回撤精度 2 位百分比
  - 盈亏比 = 平均盈利 / 平均亏损（亏损为 0 时特殊处理为 +∞）
  - 平均持仓天数精度 1 位小数
  - 无风险利率可配（默认 3%）
  - 回测指标持久化到 `QuantStrategyPerformance` 表，按 (StrategyVersion, ComboPackName, Symbol, BacktestWindow) 唯一
  - 单元测试覆盖全部 7 个指标（含盈亏比、平均持仓天数）
- **技术要点**：
  - 独立服务，与回测引擎解耦，可复用于仿真与线上监控
  - 夏普 = (E[Rp] − Rf) / σ(Rp)，年化系数 √252
  - 盈亏比 = avgWinAmount / avgLossAmount，反映每承担 1 元亏损预期获得的收益
  - 回测结果持久化后可被 MCP 接口（T26、T29）和前端（T34、T35）消费
- **边界约束（不包含）**：
  - 自定义指标扩展框架
  - Calmar / Sortino 等高级比率（P1 扩展）
  - 策略自动回测调度（属 T51）

#### T23 开发回测任务队列与去重
- **归属**：后端/运维 | **优先级**：P1 | **依赖**：T21
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Backtest.BacktestJobQueue`
  - jobId 去重逻辑（§7.4）
  - 异步执行管道（`Channel<BacktestJob>`）
- **验收标准**：
  - 重复 jobId 不产生重复计算，返回已有结果
  - 队列积压数可通过日志 / API 观测
  - 队列满时返回 HTTP 429 并携带 Retry-After 头
  - 应用重启后自动从 SQLite 恢复 Pending/Running 状态任务，Running 状态重置为 Pending 重新执行
- **技术要点**：
  - `System.Threading.Channels.Channel<T>` 实现有界队列
  - SQLite 持久化任务状态（Pending、Running、Completed、Failed）
  - `BackgroundService` 消费队列
- **边界约束（不包含）**：
  - 分布式任务调度（Hangfire / Quartz）
  - 任务优先级排序

---

### MCP 接口层阶段（T24 – T33：MCP 层）

> MCP 控制器命名空间：`SimplerJiangAiAgent.Api.Modules.Quant.Mcp.*`
> 接口定义参照 §7.2 MCP 接口表，请求/响应示例参照 §7.7。

#### T24 设计 MCP 接口 schema v1
- **归属**：后端/平台 | **优先级**：P0 | **依赖**：T18, T19
- **具体产出**：
  - 请求/响应 DTO（§7.2 全部 6 个接口），命名空间 `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.Contracts`
  - OpenAPI 文档（Swagger 注解）
  - 统一错误码枚举 `QuantErrorCode`（InvalidSymbol、InsufficientData、RiskViolation、RateLimited、IdempotencyConflict、InternalError）
  - 统一响应包装 `QuantApiResponse<T>`（含 traceId、strategyVersion、computeMs）
  - Middleware 管道注册顺序文档：Auth → RateLimit → Idempotency → CircuitBreaker → Audit
  - 健康检查端点 `GET /api/mcp/quant/health`（返回服务状态、依赖状态、版本信息）
- **验收标准**：
  - Schema 与 §7.7 三个示例的 JSON 结构完全兼容
  - 非法字段请求返回 400 + 具体字段错误信息（§8.3 Schema 防护）
  - 参数范围校验：symbol 格式、asOf 合法性、frequency 枚举值
  - OpenAPI 文档可通过 Swagger UI 浏览
  - Middleware 管道注册顺序文档化并在 Startup 中强制绑定
  - 健康检查端点返回 200 OK + 版本号 + 依赖可用性
- **技术要点**：
  - FluentValidation 实现强类型校验
  - 统一错误模型：`{ "error": { "code": "...", "message": "...", "details": [...] } }`
  - traceId / strategyVersion / dataSnapshotId 作为所有响应的公共字段
  - MCP 层 DTO（`Mcp.Contracts.*`）与内部 domain model（`Scoring.*`、`Factors.*`）通过显式 mapper 隔离，不共享类型。Controller 层负责 DTO 映射，不直接暴露内部模型
- **边界约束（不包含）**：
  - GraphQL / gRPC 接口
  - WebSocket 实时推送协议

#### T25 实现 /factors/evaluate 接口
- **归属**：后端 | **优先级**：P0 | **依赖**：T24
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.QuantFactorsController.Evaluate()`
  - 路由：`POST /api/mcp/quant/factors/evaluate`
  - 幂等键：`X-Idempotency-Key` 头（§7.2）
  - 响应结构增加 `marketConstraints` 对象（limitUp: bool, limitDown: bool, suspended: bool, st: bool）
- **验收标准**：
  - 返回因子向量（value / normalized / signal / quality），结构对齐 §7.7 示例 1
  - P95 延迟 <= 250ms（缓存命中），<= 900ms（冷算）（§2.3）
  - 缓存过期时返回最近缓存结果并标记 `stale = true`（§7.2 降级策略）
  - 权限 `quant.read`（§7.2 / §7.3）
  - 响应包含 `marketConstraints` 字段，反映标的当前 A 股约束状态；调用方可据此判断因子值是否受限
- **技术要点**：
  - 调用 T10–T17 各 `IFactorModule`，按请求的 `factors` 数组选择性计算
  - 缓存 key = `(symbol, asOf, factorKey, frequency)`
  - 幂等键 TTL 5 分钟
- **边界约束（不包含）**：
  - 批量多标的并行评估（P1 扩展）
  - 自定义因子注入

#### T26 实现 /portfolio/score 接口
- **归属**：后端 | **优先级**：P0 | **依赖**：T24, T18
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.QuantPortfolioController.Score()`
  - 路由：`POST /api/mcp/quant/portfolio/score`
  - 幂等键：`X-Idempotency-Key` 头
  - 响应结构增加 `backtestMetrics` 嵌套对象（消费 T22/T51 持久化的 `QuantStrategyPerformance`）
- **验收标准**：
  - 返回 compositeScore / rank / signal / topFactors + **backtestMetrics**（对齐 §7.7 示例 3）
  - backtestMetrics 包含：winRate、annualReturn、profitLossRatio、maxDrawdown、sharpeRatio、sampleCount（来自 T51 自动回测持久化结果）
  - backtestMetrics 无历史数据时返回 `null` 并标注 `backtestStatus = "pending"`
  - 支持 `factorSet` 参数选择组合包（§6.1），P0 支持 6 个，P1 扩展至 8 个
  - 降级为规则引擎评分时 signal 标记 `degraded = true`（§7.2 降级策略）
  - P95 延迟 <= 250ms（缓存），<= 900ms（冷算）
- **技术要点**：
  - 调用 `CompositeScorer`（T18），传入 universe 列表 + factorSet
  - strategyVersion 绑定到响应
  - 降级路径：因子服务不可用时切换为固定权重规则引擎
- **边界约束（不包含）**：
  - 实时组合持仓同步
  - 组合优化求解器（P1）

#### T27 实现 /risk/check 接口
- **归属**：后端 | **优先级**：P0 | **依赖**：T24, T19
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.QuantRiskController.Check()`
  - 路由：`POST /api/mcp/quant/risk/check`
  - 幂等键：`requestHash`（§7.2）
  - 在 `/risk/check` 响应结构中增加 `cooldowns` 数组字段（symbol、direction、expiresAt、remainingMinutes）
- **验收标准**：
  - 返回 violations 数组 + suggestion 文本（对齐 §7.7 示例 2）
  - 超时时 Fail-Close（§7.2 / §7.4）：默认收紧风险，阻断建议
  - 违规建议 100% 阻断（§2.3）
  - 权限 `quant.risk`（§7.3）
  - A 股约束全覆盖（§6.4）
  - 响应包含 `cooldowns` 数组，反映当前标的冷却状态；无冷却时为空数组
- **技术要点**：
  - 调用 `RiskGateService`（T19），传入 symbol / action / positionPct / currentPortfolio
  - `ruleVersion` 绑定到响应 trace 字段
  - requestHash 计算：对请求 body 做 SHA256 摘要
- **边界约束（不包含）**：
  - 动态风控规则热更新
  - 跨账户聚合风控

#### T28 实现 /backtest/run 接口
- **归属**：后端 | **优先级**：P1 | **依赖**：T24, T21, T23
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.QuantBacktestController.Run()`
  - 路由：`POST /api/mcp/quant/backtest/run`
  - 幂等键：`jobId`（§7.2 / §7.4）
- **验收标准**：
  - 异步提交返回 jobId + 状态查询 URL
  - 重复 jobId 返回已有任务状态而非重复计算
  - 回测不可用时降级返回预计算报告（§7.2 降级策略）
  - 权限 `quant.backtest`（§7.3）
- **技术要点**：
  - 调用 `BacktestJobQueue`（T23）提交异步任务
  - 响应 HTTP 202 Accepted + Location 头指向状态查询端点
  - `datasetVersion` 绑定到 trace
- **边界约束（不包含）**：
  - 实时流式回测进度推送（WebSocket）
  - 回测参数在线调优界面

#### T29 实现 /report/get 与 /explain/decision 接口
- **归属**：后端 | **优先级**：P0 | **依赖**：T24, T26
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.QuantReportController`
    - `Get()` → `GET /api/mcp/quant/report/get`
    - `ExplainDecision()` → `POST /api/mcp/quant/explain/decision`
  - SQLite 表 `QuantReports`（字段：Id、TraceId、ReportType、ContentJson、StrategyVersion、CreatedAt）
  - EF Core 实体 `QuantReportEntity`
- **验收标准**：
  - `/report/get` 返回结构化评分报告（含 compositeScore、topFactors、riskSummary、recommendation）
  - `/explain/decision` 返回可读自然语言解释；解释失败时返回结构化原始结果（§7.2 降级策略）
  - P0 阶段 `/explain/decision` 降级行为 = 解释失败时返回结构化原始结果即可，不要求完美自然语言输出
  - 权限：report → `quant.read`，explain → `quant.explain`（§7.3）
  - traceId / reportVersion / decisionId 绑定到响应
- **技术要点**：
  - `/explain/decision` 调用 `LlmService`（§7.8）生成可读解释
  - LLM 上下文注入策略历史回测指标（winRate、profitLossRatio、sampleCount），使 LLM 解释时可引用“该策略近 60 个交易日胜率 XX%、盈亏比 X.X”
  - LLM 只能引用引擎返回的证据字段（含回测指标），不得外推为事实（§8.3 解释约束）
  - 报告缓存：相同 traceId 的报告直接返回，不重复生成
- **边界约束（不包含）**：
  - 自定义报告模板引擎
  - 多语言解释生成

#### T30 接入鉴权、限流、配额
- **归属**：平台/后端 | **优先级**：P0 | **依赖**：T24
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.Middleware.QuantAuthMiddleware`
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.Middleware.QuantRateLimitMiddleware`
  - 权限模型实现（§7.3）：read / research / ops / admin 四级
  - 限流策略：滑动窗口限流
- **验收标准**：
  - 无权限请求返回 HTTP 403 + 错误码 `Forbidden`
  - 超限请求返回 HTTP 429 + `Retry-After` 头
  - 日志记录每次鉴权与限流判定（可审计）
  - 配额消耗可通过管理接口查询
- **技术要点**：
  - ASP.NET Core 中间件管道，针对 `/api/mcp/quant/**` 路径生效
  - 限流使用 `System.Threading.RateLimiting.SlidingWindowRateLimiter`
  - 权限存储复用项目已有的 Admin 鉴权体系
- **边界约束（不包含）**：
  - OAuth2 / OIDC 外部认证集成
  - 多租户隔离

#### T31 接入幂等键与请求去重
- **归属**：后端 | **优先级**：P0 | **依赖**：T24
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.Middleware.IdempotencyMiddleware`
  - SQLite 表 `QuantIdempotencyCache`（字段：IdempotencyKey、ResponseBody、StatusCode、CreatedAt、ExpiresAt）
- **验收标准**：
  - 相同幂等键 5 分钟内返回缓存结果，不重复计算
  - 过期后相同 key 重新计算
  - 并发相同 key 请求只执行一次（锁保护）
- **技术要点**：
  - `X-Idempotency-Key` 头用于写接口，`requestHash` 用于 risk/check（§7.2）
  - SQLite 存储 + 内存缓存双层，TTL 5 分钟
  - 并发控制使用 `SemaphoreSlim` 按 key 加锁
- **边界约束（不包含）**：
  - 分布式幂等（Redis）
  - 跨服务幂等传播

#### T32 接入全链路 trace 与审计日志
- **归属**：后端/运维 | **优先级**：P0 | **依赖**：T25, T26, T27
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.Audit.QuantAuditService`
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.Audit.QuantAuditActionFilter`（ASP.NET Core ActionFilter）
  - SQLite 表 `QuantAuditLogs`（字段：TraceId、Endpoint、RequestSummary、StrategyVersion、ParamSummary、RiskHits、OutputSummary、DurationMs、CreatedAt）
- **验收标准**：
  - 任意请求可通过 traceId 追溯完整调用链路
  - 审计记录包含 §7.5 要求的全部字段：请求摘要、策略版本、参数摘要、风险命中、输出摘要
  - 日志格式稳定，字段不随版本变化而断裂
  - 错误链路保留外层与内层异常信息（§7.5）
  - P1 接口（T28、T29）上线后增量接入 trace，不阻塞 T32 的 P0 交付
- **技术要点**：
  - ActionFilter 自动拦截 `/api/mcp/quant/**` 路径的所有请求
  - traceId 传播：请求头 → 响应头 → 日志 → 审计表
  - 结构化日志使用 `ILogger` + JSON 格式
- **边界约束（不包含）**：
  - 外部 APM 集成（Jaeger / Zipkin / Application Insights）
  - 日志全文索引

#### T33 开发降级开关与熔断策略
- **归属**：后端/运维 | **优先级**：P0 | **依赖**：T30, T32
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Mcp.Resilience.QuantCircuitBreaker`
  - 三级降级策略实现（§7.6）：
    - 一级：复杂模型不可用 → 纯规则引擎兜底
    - 二级：上游数据延迟 → 最近可信快照
    - 三级：风险评估异常 → 禁止建议，仅返回研究结论
  - 降级开关配置 `QuantDegradationConfig`
- **验收标准**：
  - 模拟上游故障时 5 秒内自动触发降级
  - 故障恢复后 30 秒内自动回升至正常模式
  - 降级状态可通过日志和管理接口观测
  - 三级降级全部有独立测试覆盖
- **技术要点**：
  - 使用 Polly `CircuitBreakerPolicy` 实现熔断
  - 降级开关支持运行时热切换（不重启服务）
  - 降级状态变更触发审计日志记录
- **边界约束（不包含）**：
  - 跨服务熔断联动
  - 自动根因分析

---

### 前端展示阶段（T34 – T38：研究工作台）

> 前端组件路径前缀：`frontend/src/modules/quant/`
> 技术栈：Vue 3 Composition API + Vite + KLineCharts

#### T34 构建研究工作台首页与策略列表
- **归属**：前端 | **优先级**：P0 | **依赖**：T26, T29
- **具体产出**：
  - `frontend/src/modules/quant/QuantWorkbench.vue`（工作台容器页）
  - `frontend/src/modules/quant/components/StrategyListPanel.vue`（策略列表面板）
  - 路由注册：`/quant` → `QuantWorkbench`
  - API 调用：`/api/mcp/quant/portfolio/score` + `/api/mcp/quant/report/get`
- **验收标准**：
  - 策略列表可加载，显示 compositeScore / rank / signal / **winRate / profitLossRatio**
  - 胜率列：显示百分比 + 颜色编码（>= 60% 绿色、40-60% 黄色、< 40% 红色）
  - 盈亏比列：显示比值 + 颜色编码（>= 2.0 绿色、1.0-2.0 黄色、< 1.0 红色）
  - 回测指标为空（`backtestStatus = "pending"`）时显示“回测中”占位符
  - 点击策略行进入信号详情（导航到 T35 面板）
  - 无阻断级错误（console.error 计数 = 0）
  - 降级态显示友好文案和可操作路径
- **技术要点**：
  - Vue 3 `<script setup>` + Composition API
  - 使用 `ActiveWatchlistService` 默认 universe（§7.8）
  - 请求状态管理：loading / success / error / degraded 四态
- **边界约束（不包含）**：
  - 策略创建 / 编辑 UI
  - 组合包参数调优界面

#### T35 构建信号解释与证据展示面板
- **归属**：前端 | **优先级**：P0 | **依赖**：T29
- **具体产出**：
  - `frontend/src/modules/quant/components/SignalExplainPanel.vue`（信号解释面板）
  - `frontend/src/modules/quant/components/EvidenceCard.vue`（证据卡片组件）
  - `frontend/src/modules/quant/components/StrategyPerformanceCard.vue`（策略历史表现卡片）
  - "建议 – 证据 – 历史表现 – 风险"四段式 UI 布局
  - API 调用：`/api/mcp/quant/explain/decision`（含回测指标上下文）
- **验收标准**：
  - 四段式 UI 完整渲染：① 评分与建议方向 ② 因子证据列表（topFactors）③ **策略历史表现**（胜率、盈亏比、年化收益、最大回撤、样本数）④ 风险提示
  - 策略历史表现卡片展示 60 日 + 120 日双窗口指标，支持窗口切换
  - 胜率 / 盈亏比使用颜色编码（与 T34 一致）+ 趋势箭头（较上次变化方向）
  - 样本数不足（< 10 笔交易）时显示⚠️“样本量不足，仅供参考”警告
  - 证据卡片只显示引擎返回的证据字段（含回测指标），不展示 LLM 外推内容（§8.3）
  - 降级态（解释失败）显示结构化原始结果，仍可操作
  - 无阻断级错误
- **技术要点**：
  - 证据白名单引用（§8.3 解释约束）：前端硬编码可展示的字段列表
  - 信号颜色映射：strong_buy → 深红、bullish → 浅红、neutral → 灰、bearish → 浅绿、strong_sell → 深绿
- **边界约束（不包含）**：
  - 自定义证据排序 / 筛选
  - 信号对比历史视图

#### T36 构建风险告警与冷却状态展示
- **归属**：前端 | **优先级**：P0 | **依赖**：T27, T20
- **具体产出**：
  - `frontend/src/modules/quant/components/RiskAlertPanel.vue`（风险告警面板）
  - `frontend/src/modules/quant/components/CooldownStatus.vue`（冷却状态组件）
  - API 调用：`/api/mcp/quant/risk/check`
- **验收标准**：
  - 风险违规显示 violations 列表（规则名、限额、实际值、操作 block/pass）
  - suggestion 文本完整展示
  - 冷却倒计时可见（剩余分钟数），过期后自动刷新状态
  - A 股约束可视化提示：T+1 限制、涨跌停状态、停牌提醒（§6.4）
- **技术要点**：
  - 冷却状态轮询间隔 30 秒
  - 违规级别颜色：block → 红色、warning → 橙色、pass → 绿色
- **边界约束（不包含）**：
  - 实时 WebSocket 推送
  - 风险规则管理 UI

#### T37 构建回测报告可视化页面
- **归属**：前端 | **优先级**：P1 | **依赖**：T28, T22
- **具体产出**：
  - `frontend/src/modules/quant/BacktestReport.vue`（回测报告页）
  - 收益曲线图（KLineCharts 折线图）
  - 指标摘要表（年化收益、夏普、最大回撤、胜率、换手率）
  - API 调用：`/api/mcp/quant/backtest/run` + `/api/mcp/quant/report/get`
- **验收标准**：
  - 收益曲线图正确渲染，时间轴 / 收益轴可缩放
  - 指标表 5 个指标（§10.1）全部展示，精度对齐 T22
  - 异步任务轮询：提交后显示进度，完成后自动加载报告
  - 无阻断级 JS 错误
- **技术要点**：
  - KLineCharts 折线图模式渲染收益曲线
  - 异步轮询间隔：前 10 次 2 秒，之后 10 秒，最长等待 5 分钟
- **边界约束（不包含）**：
  - 交互式参数调优回测
  - 多策略对比图

#### T38 构建 trace 检索与审计页面
- **归属**：前端 | **优先级**：P1 | **依赖**：T32
- **具体产出**：
  - `frontend/src/modules/quant/TraceAuditPage.vue`（审计检索页）
  - traceId 搜索框 + 链路详情展示
  - 版本比对视图（StrategyVersion 变更对照）
- **验收标准**：
  - traceId 搜索返回完整链路记录（请求摘要、策略版本、参数摘要、风险命中、输出摘要、耗时）
  - 分页加载，每页 20 条，支持按时间范围筛选
  - 版本比对：选择两个 traceId 后并排展示差异
- **技术要点**：
  - 分页 API 调用 + 无限滚动
  - 结构化日志格式化展示（JSON → 可折叠树形视图）
- **边界约束（不包含）**：
  - 实时日志流（WebSocket tailing）
  - 日志导出功能

---

### Research Pipeline 集成阶段（T46 – T50：系统集成）

> 承接 §7.8 定义的 10 个集成点，将量化引擎 MCP 能力注入现有 Research Pipeline 和交易系统。

#### T46 ResearchRunner 新增 QuantFactorStage 阶段
- **归属**：后端 | **优先级**：P0 | **依赖**：T25, T26
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Research.Stages.QuantFactorStage`（实现 `IResearchStage`）
  - ResearchRunner 注册 QuantFactorStage 到流程管道
  - 阶段输出：因子评估结果作为 evidence 注入 ResearchSession
- **验收标准**：
  - Research Pipeline 执行时自动调用 `/api/mcp/quant/factors/evaluate`
  - 因子评估结果以结构化 evidence block 形式存入 ResearchSession.Turn
  - QuantFactorStage 不可用时降级跳过，不阻塞 Research Pipeline 其余阶段
  - 单元测试覆盖：正常注入、降级跳过、因子评估超时
- **技术要点**：
  - 复用现有 `ResearchRunner` 阶段管道模式（§7.8 集成点 1）
  - 超时兜底：因子评估 > 3 秒时跳过并记录告警
- **边界约束（不包含）**：
  - 修改 ResearchRunner 核心架构
  - 因子结果自动替代 LLM 分析

#### T47 ResearchRoleExecutor 注册 Quant MCP 工具
- **归属**：后端 | **优先级**：P0 | **依赖**：T24, T25, T26, T27
- **具体产出**：
  - Agent 工具注册：`QuantFactorsEvaluateTool`、`QuantPortfolioScoreTool`、`QuantRiskCheckTool`
  - 工具 schema 定义（JSON Schema，供 LLM 函数调用）
  - 权限绑定：agent 工具继承 §7.3 权限模型
- **验收标准**：
  - analyst / commander 角色在 Research 流程中可调用 quant 评分工具
  - LLM 函数调用 schema 通过校验，非法参数被拒绝
  - 工具执行结果正确注入 ResearchSession.Turn
  - 单元测试覆盖：工具注册、调用成功、参数校验失败
- **技术要点**：
  - 复用现有 `ResearchRoleExecutor` 工具注册模式（§7.8 集成点 2）
  - 工具描述文本需清晰告知 LLM 每个工具的能力边界
- **边界约束（不包含）**：
  - 自动调用策略编排（LLM 自主决定何时调用）
  - 回测工具暴露给 agent（P1，T28 完成后扩展）

#### T48 StockStrategyMcp 信号对齐校验
- **归属**：后端 | **优先级**：P1 | **依赖**：T18, T25
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Integration.SignalAlignmentService`
  - 对比逻辑：现有 TD Sequential / MACD 信号与量化因子信号一致性校验
  - 输出：一致性报告（agreement_ratio、conflict_list）
- **验收标准**：
  - 同一标的的已有信号（TD/MACD）与量化因子信号做方向对比
  - 冲突时在评分报告中标注"信号不一致"警告
  - agreement_ratio 计算正确（数值测试验证）
- **技术要点**：
  - 消费 `StockStrategyMcp` 已有信号输出（§7.8 集成点 6）
  - 对齐逻辑：bullish 系 vs bearish 系方向比较，neutral 不参与对齐
- **边界约束（不包含）**：
  - 信号融合（将两套信号合并为单一评分）
  - 修改已有 StockStrategyMcp 逻辑

#### T49 TradingPlan 事件联动
- **归属**：后端 | **优先级**：P1 | **依赖**：T18, T19
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Integration.TradingPlanEventTrigger`
  - 事件触发：因子评分显著变化（|Δscore| > 阈值）时发布 TradingPlanReviewEvent
  - 事件消费：TradingPlan 模块订阅并触发计划复核流程
- **验收标准**：
  - 评分变化超过阈值时成功触发复核事件
  - 事件包含：symbol、oldScore、newScore、triggeredFactors、traceId
  - 阈值可配置（默认 |Δscore| > 0.5）
  - 单元测试覆盖：阈值触发、阈值未触发、事件结构正确
- **技术要点**：
  - 使用 MediatR `INotification` 或项目已有事件总线模式（§7.8 集成点 8）
  - TradingPlan 模块消费端为已有代码，本任务只负责发布端
- **边界约束（不包含）**：
  - TradingPlan 复核逻辑改造
  - 自动生成交易计划

#### T50 盘中增量因子刷新 Worker
- **归属**：后端 | **优先级**：P1 | **依赖**：T10, T11, T12, T13, T14, T18
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Workers.IncrementalFactorRefreshWorker`（BackgroundService）
  - 盘前加载当日 universe（来自 ActiveWatchlistService）
  - 盘中按分钟间隔增量刷新因子（消费 HighFrequencyQuoteService）
  - 仅在 A 股交易时段运行（9:30-11:30, 13:00-15:00）
- **验收标准**：
  - 交易时段内每分钟刷新 watchlist 标的的因子值
  - 非交易时段自动休眠，不消耗 CPU
  - 集合竞价时段（9:15-9:25, 14:57-15:00）数据标记为 `AuctionPeriod`（§6.4）
  - 刷新延迟 P95 <= 5 秒（300 标的规模）
- **技术要点**：
  - 消费 `HighFrequencyQuoteService` 实时行情（§7.8 集成点 9）
  - 消费 `ActiveWatchlistService` 作为 universe（§7.8 集成点 7）
  - A 股交易时段 gate：仅在 CST 交易时段激活
- **边界约束（不包含）**：
  - 实时 tick 级因子计算
  - 全市场 3000+ 标的扫描

#### T51 策略/组合包自动回测与指标持久化
- **归属**：后端 | **优先级**：P0 | **依赖**：T18, T21, T22
- **具体产出**：
  - `SimplerJiangAiAgent.Api.Modules.Quant.Backtest.StrategyAutoBacktestWorker`（BackgroundService）
  - 自动回测调度：每日盘后（15:30 CST）对所有活跃组合包 × watchlist 标的执行回测
  - 回测窗口：默认近 60 个交易日 + 近 120 个交易日双窗口
  - 指标持久化到 `QuantStrategyPerformance` 表（T22 产出）
  - 回测摘要查询接口 `IStrategyPerformanceStore.GetLatest(comboPackName, symbol)`
- **验收标准**：
  - 每日盘后自动触发回测，覆盖全部活跃组合包 × watchlist 标的
  - 回测结果持久化，包含：胜率（winRate）、年化收益（annualReturn）、盈亏比（profitLossRatio）、最大回撤（maxDrawdown）、夏普比率（sharpeRatio）、样本交易数（sampleCount）、平均持仓天数（avgHoldingDays）
  - 60 日窗口 + 120 日窗口双维度指标均持久化
  - 新增因子（§6.5 扩展性架构）上线后自动纳入下一次回测周期
  - 非交易日不触发回测
  - 回测失败（数据不足 / 引擎异常）记录错误日志但不阻塞其他标的回测
  - 单元测试覆盖：调度触发、指标持久化、数据不足降级、新因子自动纳入
- **技术要点**：
  - `BackgroundService` + `CronExpression` 定时调度（工作日 15:30 CST）
  - 并行回测：`Parallel.ForEachAsync` 控制并发度（默认 4 线程）
  - 消费 `ActiveWatchlistService` 作为标的 universe（§7.8 集成点 7）
  - 遍历 `FactorRegistry`（§6.5）获取所有活跃因子和组合包
  - 回测参数快照绑定 `StrategyVersion`（T03），确保可复现
  - 增量模式：仅回测自上次回测以来有新数据的标的，减少重复计算
- **边界约束（不包含）**：
  - 实时盘中回测（仅盘后离线回测）
  - 回测参数自动优化（属 P1 参数巡检）
  - 跨组合包对比排名

---

### 测试与验证阶段（T39 – T43：测试）

#### T39 完成因子与评分单元测试
- **归属**：测试/后端 | **优先级**：P0 | **依赖**：T10, T11, T12, T13, T14, T15, T18, T19, T20
- **具体产出**：
  - `backend/SimplerJiangAiAgent.Api.Tests/Quant/QuantFactorTests.cs`（8 个因子模块测试）
  - `backend/SimplerJiangAiAgent.Api.Tests/Quant/CompositeScorerTests.cs`（评分聚合器测试）
  - `backend/SimplerJiangAiAgent.Api.Tests/Quant/RiskGateTests.cs`（风险闸门测试）
  - `backend/SimplerJiangAiAgent.Api.Tests/Quant/CooldownServiceTests.cs`（冷却机制测试）
- **验收标准**：
  - P0 因子与评分单元测试，覆盖 T10-T15 + T18-T20；P1 因子（T16/T17）测试随 T16/T17 完成后补充
  - 合成 K 线样本 fixtures：至少包含上涨趋势、下跌趋势、震荡、极端值四种场景
  - 确定性输出断言：相同输入必须产生完全相同的输出
  - 0 P0 缺陷（§4.3 测试验收标准）
- **技术要点**：
  - xUnit + FluentAssertions
  - 使用合成数据 fixtures 而非实盘数据，保证可复现
  - 因子预热期不足场景必须覆盖
- **边界约束（不包含）**：
  - 性能基准测试（T42）
  - 集成测试（T41）

#### T40 完成 MCP 契约测试
- **归属**：测试 | **优先级**：P0 | **依赖**：T25, T26, T27, T28, T29, T30, T31
- **具体产出**：
  - `backend/SimplerJiangAiAgent.Api.Tests/Quant/QuantMcpContractTests.cs`
  - 覆盖 §7.2 全部 6 个 MCP 接口
- **验收标准**：
  - 契约测试通过率 100%
  - 覆盖维度：正常请求、非法参数、鉴权失败、限流触发、幂等重放、降级场景
  - Schema 验证：响应 JSON 结构与 §7.7 示例一致
  - 错误码验证：全部 `QuantErrorCode` 枚举值可触发
- **技术要点**：
  - `WebApplicationFactory<Program>` 集成测试
  - 使用 `System.Text.Json` schema 验证
  - 幂等测试：连续两次相同请求，第二次直接返回缓存
- **边界约束（不包含）**：
  - 第三方消费者驱动契约（Pact）
  - 跨服务契约测试

#### T41 完成端到端集成测试
- **归属**：测试 | **优先级**：P0 | **依赖**：T34, T35, T36
- **具体产出**：
  - `frontend/src/modules/quant/__tests__/quant-e2e.spec.js`
  - 覆盖研究工作台关键流程
- **验收标准**：
  - P0 前端链路 E2E 测试：策略列表加载 → 信号详情查看 → 风险检查全链路通过；P1 页面（T37/T38）E2E 随各自完成后补充
  - 无阻断级缺陷，console.error 计数 = 0
  - 降级场景 UI 可操作：后端不可用时显示友好提示
- **技术要点**：
  - Browser MCP 验证，CopilotBrowser 优先（按项目规范）
  - 后端服务必须先启动并确认健康
  - 测试前确认 `/api/mcp/quant/portfolio/score` 可响应
- **边界约束（不包含）**：
  - 跨浏览器兼容性测试
  - 移动端响应式测试

#### T42 完成性能压测与瓶颈定位
- **归属**：测试/运维 | **优先级**：P1 | **依赖**：T40, T41
- **具体产出**：
  - 压测脚本（k6 或 bombardier）
  - 压测报告（含 P50 / P95 / P99 延迟、吞吐量、错误率）
  - 瓶颈分析报告（标注 top-3 瓶颈 + 优化建议）
- **验收标准**：
  - `/portfolio/score` P95 <= 250ms（缓存命中），<= 900ms（冷算）（§2.3）
  - `/risk/check` P95 <= 100ms
  - 并发 300 RPS 无错误（§9.1 负载假设）
  - 错误率 <= 0.2%（§10.1 工程指标）
- **技术要点**：
  - 分场景压测：缓存命中 vs 缓存失配 vs 冷启动
  - 数据准备：预填充 1000 标的特征数据
- **边界约束（不包含）**：
  - 全量生产环境压测
  - 自动化容量规划

#### T43 完成故障注入与演练
- **归属**：测试/运维 | **优先级**：P1 | **依赖**：T33, T42
- **具体产出**：
  - 故障演练剧本（覆盖 §12 R1–R5 风险场景）
  - 故障演练报告（每场景：触发 → 降级 → 恢复 → 验证）
  - 回滚脚本验证记录
- **验收标准**：
  - 上游超时、坏数据、重复请求、权限异常场景全覆盖（§4.3）
  - 三级降级（§7.6）全部可触发且可恢复
  - 回滚流程 15 分钟内完成（§4.4 运维验收标准）
  - 关键告警 5 分钟内可触发（§4.4）
- **技术要点**：
  - 模拟依赖故障：注入超时、错误响应、数据缺失
  - 验证降级状态转换：正常 → 一级 → 二级 → 三级 → 恢复
- **边界约束（不包含）**：
  - 混沌工程平台（Chaos Monkey）
  - 生产环境故障注入

---

### 发布上线阶段（T44 – T45：上线）

#### T44 完成灰度发布与 A/B 实验配置
- **归属**：运维/产品 | **优先级**：P1 | **依赖**：T43
- **具体产出**：
  - 灰度策略配置文件（§11.1 四阶段：5% → 20% → 50% → 100%）
  - A/B 分组配置（§11.2）：A 组规则引擎基线 / B 组因子引擎 + LLM 解释
  - 特性开关实现 `SimplerJiangAiAgent.Api.Modules.Quant.Config.QuantFeatureFlags`
- **验收标准**：
  - 灰度比例可动态调整（不重启服务）
  - A/B 分组指标可观测：收益风险比、信号稳定性、用户采纳率、误用率（§11.2）
  - 灰度回退到 0% 后无残留流量
  - 特性开关状态可通过管理接口查询
- **技术要点**：
  - 策略版本路由：根据灰度比例路由到不同 `StrategyVersion`
  - 分流基于用户 ID 哈希，保证同一用户稳定分组
- **边界约束（不包含）**：
  - 自动放量决策（需人工审批）
  - 多维度分层实验

#### T45 完成上线评审与放量决策
- **归属**：产品/技术委员会 | **优先级**：P0 | **依赖**：T44
- **具体产出**：
  - 上线评审包（§3.2 M5 里程碑）：试运行报告 + 指标达标证明 + 风险复盘
  - Runbook 文档（§4.4）：值班流程、升级路径、回溯流程
  - 回滚预案（§11.3）：触发条件 + 回滚动作 + 数据修复方案
- **验收标准**：
  - §2.3 全部验收门槛达标：接口成功率 >= 99.9%、P95 延迟达标、回测误差 < 0.1%、风险闸门 100% 阻断
  - 回滚预案演练通过（15 分钟内回滚完成）
  - 运营与技术共同签字（§3.2 M5 放行条件）
  - 监控仪表盘就绪：§10.1 全部指标类别可观测
- **技术要点**：
  - 评审清单逐项检查，缺一不可
  - 上线后 72 小时监控增强期：告警阈值收紧 50%
- **边界约束（不包含）**：
  - 全量放量执行（评审通过后独立操作）
  - 后续迭代规划

---

## 14. PM 执行建议（下一步 7 天）

### Day 1-2：冻结与建模
1. 召开跨职能冻结会，确认 P0 边界、风险闸门、上线门槛。
2. 输出需求冻结版本与指标字典，建立变更审批规则。

### Day 3-4：数据与接口先行
1. 确认数据源可用性与时效 SLA，明确缺口和替代源。
2. 完成 MCP schema v1 与错误码约定评审。

### Day 5：技术路线拍板
1. 决策主线采用“因子引擎 + 优化器”，规则引擎为兜底。
2. 明确 LLM 只做解释与编排，不参与评分裁决。

### Day 6：排期与派工
1. 按本文 T01-T45 建立项目看板，补全 owner 与预计工时。
2. 对 P0 任务设置每日站会跟踪和阻塞升级机制。

### Day 7：启动检查点
1. 完成 W1 里程碑复核，确认 W2 进入条件。
2. 发布第一版周报模板：进度、风险、质量指标、决策事项。

---

## 附录 A：建议的放行门槛（供评审会直接引用）
- 功能放行：P0 范围全部完成，且无阻断级缺陷。
- 质量放行：关键接口成功率、延迟、错误率达标。
- 风险放行：风险闸门误放行为 0，冷却机制正常生效。
- 运营放行：灰度、回滚、应急 Runbook 全部演练通过。

## 附录 B：文档使用说明
- 本文是执行基线，后续仅允许通过版本化变更单更新。
- 任何新增能力必须先映射到 P0/P1/P2 范围，再进入开发排期。
