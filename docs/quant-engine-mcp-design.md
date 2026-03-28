# Quant Engine + MCP 可执行设计文档

## 1. 背景与目标

### 1.1 为什么量化引擎优先于 RAG

在交易与信号生成场景中，核心问题是“可重复计算、可验证回测、可在线迭代”。
RAG（Retrieval-Augmented Generation）擅长语义检索与解释，但对以下能力天然较弱：

- 数值稳定性：同一输入需稳定输出同一信号与评分。
- 可审计性：因子值、打分过程、组合权重必须可追溯。
- 低延迟在线更新：分钟级/秒级增量计算和状态维护。
- 策略可回放：严格复盘历史信号与交易行为。

量化引擎优先，不代表放弃 RAG。量化引擎负责“算分与决策框架”，RAG 负责“解释与补充上下文”。

### 1.2 目标

- 建立可插拔量化引擎，支持多市场、多频率（1m/5m/day）因子计算。
- 通过 MCP 暴露统一 API，供前端、策略代理、自动化流程调用。
- 在现有仓库上最小侵入接入，不替换已有 MCP 能力。
- 形成可交付 MVP（2 周）并在 12 周内演进为可生产化方案。

### 1.3 边界

- 本阶段不实现实盘自动下单，仅提供“信号与建议仓位”。
- 不引入高频毫秒撮合模型，先聚焦分钟级与日频。
- 不把 RAG 用于直接打分，只允许用于解释、事件补全与质检。

## 2. 与当前 MCP 体系的关系

Quant MCP 层是“新增层”，不是替代层。

- 现有 MCP：偏向数据查询、分析、系统管理、AI 辅助。
- Quant MCP：新增“因子计算、组合评分、回测仿真、在线监控”能力。
- 调用关系：上层 Agent/前端通过 MCP 调 Quant 能力，Quant 再调用现有行情/新闻/数据库接口。

建议分层：

1. Existing MCP（保持不变）
2. Quant MCP Adapter（协议层、权限、参数校验）
3. Quant Engine Core（计算与状态）
4. Data Connectors（接入当前仓库服务）

## 3. 量化引擎总体架构

| 层级 | 主要职责 | 输入 | 输出 | 技术建议 |
|---|---|---|---|---|
| 数据层 Data | 统一行情/财务/新闻/资金流接入与清洗 | 现有 API、DB、外部源 | 标准化时序数据 | C# 后端服务 + 缓存 |
| 特征层 Feature | 构建 rolling 特征、截面特征 | OHLCV、盘口、新闻事件 | 特征矩阵 X | 批流一体（增量优先） |
| 因子层 Factor | 单因子计算、行业中性化、缺失处理 | 特征矩阵 | 因子矩阵 F | 插件化因子注册表 |
| 组合层 Portfolio | 多因子合成、权重优化、约束求解 | F、风险模型 | 组合分数/目标权重 | 线性约束 + 风险预算 |
| 执行层 Execution | 交易规则仿真（T+1、涨跌停等） | 目标权重、行情状态 | 订单建议与成交模拟 | 事件驱动状态机 |
| 评估层 Evaluation | 回测评估、归因、稳定性监控 | 策略轨迹、基准 | 指标、报表、告警 | 指标库 + 可观测性 |

关键设计原则：

- 计算可重放：给定日期与版本，结果可复现。
- 因子可插拔：每个因子独立声明依赖、频率、参数。
- 在线离线一致：离线回测与在线推理复用同一计算图。

## 4. 因子库设计

### 4.1 因子分类与清单（>= 20 个）

| 类别 | 因子名 | 简述 | 频率 | 方向 |
|---|---|---|---|---|
| 趋势 | MA_Cross_5_20 | 短长均线金叉/死叉强度 | 1m/day | 双向 |
| 趋势 | EMA_Slope_20 | 20 期 EMA 斜率 | 1m/day | 正向 |
| 趋势 | ADX_14 | 趋势强度指标 | day | 正向 |
| 趋势 | Donchian_Break_20 | 唐奇安通道突破幅度 | day | 正向 |
| 动量 | Ret_1D | 1 日收益率 | day | 正向 |
| 动量 | Ret_5D | 5 日收益率 | day | 正向 |
| 动量 | RSI_14 | 相对强弱指数 | 1m/day | 反转/阈值 |
| 动量 | MACD_Hist | MACD 柱值 | 1m/day | 双向 |
| 波动 | RealizedVol_20 | 20 期实现波动率 | day | 负向 |
| 波动 | ATR_14 | 真实波幅均值 | day | 负向 |
| 波动 | ParkinsonVol | 高低价区间波动估计 | day | 负向 |
| 成交量 | Volume_Surge_5 | 近 5 期放量倍数 | 1m/day | 正向 |
| 成交量 | OBV_Trend | 能量潮趋势斜率 | day | 正向 |
| 成交量 | Turnover_Rank | 换手率截面分位 | day | 双向 |
| 资金流 | NetMainInflow_1D | 主力净流入日值 | day | 正向 |
| 资金流 | NetMainInflow_5D | 主力净流入 5 日累计 | day | 正向 |
| 资金流 | LargeOrderRatio | 大单成交占比 | intraday | 正向 |
| 横截面 | IndustryNeutral_Mom | 行业中性动量 | day | 正向 |
| 横截面 | Size_Adjusted_Alpha | 市值中性 alpha | day | 正向 |
| 横截面 | Residual_Reversal | 残差反转因子 | day | 反向 |
| 事件情绪 | NewsSent_72h | 72 小时新闻情绪分 | 1h/day | 正向 |
| 事件情绪 | EventShock_Decay | 事件冲击衰减得分 | 1h/day | 双向 |
| 事件情绪 | PolicyTone | 政策语义偏多偏空得分 | day | 双向 |
| 风险暴露 | Beta_60 | 对基准 60 期 beta | day | 约束 |
| 风险暴露 | Size_Exposure | 市值暴露 | day | 约束 |
| 风险暴露 | Liquidity_Exposure | 流动性暴露 | day | 约束 |
| 风险暴露 | Sector_Exposure | 行业偏离暴露 | day | 约束 |

### 4.2 因子元数据规范

每个因子需定义：

- factorKey: 唯一键（如 MOM_RET_5D）
- category: 所属类别
- requiredFields: 依赖字段
- warmupBars: 预热长度
- updateMode: batch 或 incremental
- neutralization: 行业/市值中性化规则
- clipRule: winsorize 配置
- version: 语义化版本（如 1.2.0）

## 5. 因子标准化与组合打分方法

### 5.1 标准化流程

1. 缺失值处理：按行业中位数填充，缺失占比超阈值直接剔除。
2. 去极值：MAD 或分位截断（1%/99%）。
3. 标准化：
   - z-score：适合近似正态因子。
   - rank-normalize：对重尾分布更稳健。
4. 中性化：对行业、市值、beta 做截面回归残差化。

### 5.2 质量评估

- IC（Information Coefficient）：日/周滚动 Spearman IC。
- IR（Information Ratio）：IC 均值/IC 标准差。
- 稳定性：IC 正值占比、分层收益单调性。

### 5.3 组合打分

- 基础打分：Score = Sum(w_i * f_i_norm)
- 权重初值：按 IR 或 ICIR 分配。
- 去相关：相关矩阵阈值过滤（例如 |rho| > 0.7 仅保留更稳健者）。
- 约束：
  - 单因子权重上限（如 <= 15%）
  - 行业偏离约束（如 <= 基准权重 +/- 5%）
  - 风格暴露约束（beta/size/liquidity）

## 6. 回测与仿真框架

### 6.1 回测核心

- 事件驱动日历：开盘、收盘、财报、停牌复牌、除权除息。
- 交易成本模型：佣金 + 印花税 + 冲击成本。
- 滑点模型：按成交额分桶或盘口深度近似。
- 约束模型：
  - 涨跌停不可成交
  - 停牌不可成交
  - A 股 T+1 卖出约束
  - 最小交易单位（手）

### 6.2 回测输出

- 收益指标：年化收益、超额收益、夏普、索提诺。
- 风险指标：最大回撤、波动率、下行波动。
- 交易指标：换手率、胜率、盈亏比、容量估计。
- 归因指标：按因子、行业、风格拆解贡献。

## 7. 在线推理流程

1. 盘前：加载当日 universe、前一日状态、模型版本。
2. 盘中增量更新：分钟级更新行情与资金流，仅重算受影响因子。
3. 打分刷新：按触发器（时间/价格跳变/成交量突变）刷新组合分。
4. 信号治理：阈值、冷却时间、最小变更仓位。
5. 因子失效检测：滚动 IC 连续低于阈值触发降权或下线。
6. 漂移监控：
   - 数据漂移（PSI、KS）
   - 收益漂移（实时 hit ratio）
   - 行为漂移（换手异常）
7. 回写与审计：写入 signal log、factor snapshot、decision trace。

## 8. MCP API 设计

### 8.1 API 端点清单（>= 10）

| 序号 | Method | Endpoint | 功能 | 请求示例（简） | 响应字段（核心） | 典型错误码 |
|---|---|---|---|---|---|---|
| 1 | POST | /api/mcp/quant/factors/calculate | 计算单/多因子 | symbol, asOf, factors | factorValues, qualityFlags | Q4001, Q5001 |
| 2 | POST | /api/mcp/quant/factors/batch-calculate | 批量 universe 因子计算 | symbols, asOf, factorSet | rows, latencyMs | Q4002, Q5002 |
| 3 | GET | /api/mcp/quant/factors/catalog | 因子目录与元数据 | category?, freq? | factors[], versions | Q4041 |
| 4 | POST | /api/mcp/quant/score/compose | 多因子组合打分 | universe, weights, constraints | scores[], diagnostics | Q4003, Q4101 |
| 5 | POST | /api/mcp/quant/portfolio/optimize | 权重优化 | target, riskModel, limits | weights, riskExposure | Q4201, Q5003 |
| 6 | POST | /api/mcp/quant/backtest/run | 运行回测 | strategySpec, dateRange | pnlSeries, metrics, trades | Q4301, Q5004 |
| 7 | GET | /api/mcp/quant/backtest/{jobId} | 查询回测任务结果 | jobId | status, resultUrl, metrics | Q4042 |
| 8 | POST | /api/mcp/quant/simulate/order | 仿真执行 | targetWeights, marketState | fills, slippageCost | Q4401 |
| 9 | GET | /api/mcp/quant/monitor/drift | 查看漂移监控 | modelId, window | psi, ks, icTrend | Q4501 |
| 10 | POST | /api/mcp/quant/monitor/factor-degrade | 因子失效检测 | factorKey, lookback | degradeLevel, action | Q4502 |
| 11 | GET | /api/mcp/quant/signals/latest | 获取最新信号 | symbol/universe | signal, confidence, reason | Q4043 |
| 12 | GET | /api/mcp/quant/health | Quant 引擎健康检查 | none | status, deps, lagMs | Q5000 |

### 8.2 请求与响应示例

请求示例：组合打分

```json
POST /api/mcp/quant/score/compose
{
  "asOf": "2026-03-28T10:30:00+08:00",
  "universe": ["600519", "000001", "300750"],
  "factorSet": "core_v1",
  "weights": {
    "MOM_RET_5D": 0.2,
    "TREND_EMA_SLOPE_20": 0.2,
    "VOL_REALIZED_20": -0.1,
    "FLOW_MAIN_NET_5D": 0.25,
    "NEWS_SENT_72H": 0.15,
    "RESIDUAL_REV": 0.1
  },
  "constraints": {
    "maxSingleFactorWeight": 0.15,
    "maxTurnover": 0.35,
    "sectorDeviation": 0.05
  }
}
```

响应示例：

```json
{
  "traceId": "q-20260328-103000-001",
  "asOf": "2026-03-28T10:30:00+08:00",
  "scores": [
    { "symbol": "600519", "score": 1.84, "rank": 1 },
    { "symbol": "300750", "score": 0.92, "rank": 2 },
    { "symbol": "000001", "score": -0.31, "rank": 3 }
  ],
  "diagnostics": {
    "ic": 0.062,
    "ir": 0.79,
    "turnoverEst": 0.28,
    "factorCoverage": 0.97
  },
  "errors": []
}
```

### 8.3 错误码规范

| 错误码 | 含义 | 处理建议 |
|---|---|---|
| Q4001 | 参数非法（字段缺失/类型错误） | 返回字段级校验信息 |
| Q4002 | 请求规模超限 | 提示拆批或缩小 universe |
| Q4101 | 权重约束冲突 | 返回冲突约束详情 |
| Q4201 | 优化器不可行 | 放宽约束或切换目标函数 |
| Q4301 | 回测日期区间非法 | 校正交易日范围 |
| Q4401 | 交易仿真市场状态缺失 | 补齐停牌/涨跌停状态 |
| Q4501 | 漂移窗口不足 | 提示最小窗口 |
| Q5000 | 服务内部错误 | 记录 traceId，支持重试 |
| Q5001 | 因子计算失败 | 返回失败因子列表 |
| Q5004 | 回测执行异常 | 降级返回部分结果 |

## 9. 与现有仓库可对接点

基于当前仓库结构，建议优先复用以下模块与接口能力（命名以现有项目为准）：

- 行情与高频：HighFrequencyQuoteService、RealtimeMarketOverviewService、RealtimeSectorBoardService
- 股票详情与缓存：/api/stocks/detail/cache（已有快速路径）
- 观察池：ActiveWatchlistService（可作为默认 universe 来源）
- 新闻与本地事实：LocalFactIngestionService、LocalFactAiEnrichmentService、/api/news 系列
- LLM 配置与审计：JsonFileLlmSettingsStore、LlmService（仅用于解释层）
- 数据访问：backend/SimplerJiangAiAgent.Api/Data 与 AppDbContext
- 测试基建：backend/SimplerJiangAiAgent.Api.Tests 现有单测体系

对接原则：

- Quant 引擎先消费现有数据服务，不直接绕开业务层访问外部源。
- 新增 Quant 模块保持独立命名空间，避免污染既有 API。

## 10. 资源与容量估算

### 10.1 容量分档

| 档位 | Universe 规模 | 频率 | 存储（30 天） | CPU 建议 | 内存建议 | 适用阶段 |
|---|---:|---|---:|---|---|---|
| 小 | 300 标的 | 日频 + 5m | 30-60 GB | 8 vCPU | 16 GB | MVP 试点 |
| 中 | 1500 标的 | 1m + 日频 | 200-400 GB | 16-32 vCPU | 64 GB | 团队内测 |
| 大 | 5000 标的 | 1m + 多因子回放 | 1-2 TB | 64 vCPU+ | 128-256 GB | 生产规模 |

### 10.2 关键性能目标

- 单次 1500 标的 x 30 因子增量刷新 < 3 秒
- 单日回测（3 年窗口）在中档资源下 < 20 分钟
- API P95 延迟：查询类 < 300ms，计算类 < 1500ms

## 11. 实施路线图

| 阶段 | 时间 | 目标 | 主要交付 |
|---|---|---|---|
| MVP | 2 周 | 跑通最小闭环 | 10 个核心因子、基础打分、单策略回测、3 个核心 API |
| 增强 | 6 周 | 提升稳定性与覆盖 | 20+ 因子、约束优化、漂移监控、12 个 API、基础可视化 |
| 成熟 | 12 周 | 生产化能力 | 多策略并行、任务调度、分层缓存、完整审计与告警 |

里程碑建议：

- W1-W2：数据标准层 + 因子注册表 + 回测 MVP
- W3-W6：优化器、执行仿真、在线增量更新
- W7-W12：漂移治理、容量扩展、SLO 与灰度机制

## 12. 风险与防呆

| 风险 | 典型表现 | 防呆机制 |
|---|---|---|
| 过拟合 | 训练优异、实盘失真 | 时序分层验证、滚动窗口、参数简化 |
| 数据窥探 | 用到未来信息 | 严格按时间戳切分，特征延迟对齐 |
| 幸存者偏差 | 只看存活标的 | 历史 universe 快照、退市样本纳入 |
| 未来函数 | 回测收益异常高 | 审计字段中保留 dataAvailableAt |
| 高换手幻觉 | 毛收益高净收益低 | 强制成本/冲击模型，换手上限约束 |
| 因子拥挤 | 同质因子集体失效 | 去相关与拥挤度监控，动态降权 |

## 13. 验收指标

### 13.1 收益类

- 超额收益（相对基准）
- 信息比率 IR
- 最大回撤控制在阈值内

### 13.2 稳定性

- 滚动 6 个月 IC 为正占比 > 60%
- 策略月胜率达标（按产品目标设阈）
- 换手率与冲击成本不超过预算

### 13.3 工程指标

- API 可用性 >= 99.5%
- 回测任务成功率 >= 99%
- 关键接口 P95 延迟达标
- 全链路 traceId 覆盖率 100%

## 14. 最小可行任务清单（可直接派工）

### 14.1 后端

- 新建 Quant Engine 模块骨架（Data/Feature/Factor/Score/Backtest）
- 实现 10 个 MVP 因子与注册表
- 新增 6 个核心 MCP 端点（先覆盖计算、打分、回测、健康）
- 接入现有行情与 watchlist 服务
- 增加因子快照与信号审计表

### 14.2 前端

- 增加 Quant 控制台页：因子面板、打分榜、回测结果
- 增加策略配置表单（权重、约束、回测区间）
- 增加漂移监控可视化（IC 曲线、PSI 热力）

### 14.3 测试

- 单元测试：因子计算、标准化、约束求解
- 回测一致性测试：固定数据快照下结果不可漂移
- API 合约测试：请求校验、错误码、字段完整性
- 浏览器 MCP 测试：核心操作链路可点击、无报错

### 14.4 运维

- 配置任务队列与定时调度（盘前初始化、盘中增量）
- 配置缓存与冷热分层存储
- 设定 SLO、告警规则与容量阈值
- 发布灰度策略（按用户/策略分组）

## 15. 决策建议：何时量化引擎优先，何时保留 RAG 辅助

### 15.1 量化引擎优先

- 目标是稳定信号生成、可回测、可审计。
- 需要分钟级增量更新和明确风险约束。
- 需要把结果纳入自动化执行或准自动执行。

### 15.2 保留 RAG 辅助

- 需要解释“为什么给出该信号”的自然语言说明。
- 需要补充结构化数据之外的事件语义（政策、突发新闻）。
- 需要面向用户生成研究摘要与问答，而非直接决策。

### 15.3 推荐混合模式

- Quant 负责 score 和 rank（决策主路径）。
- RAG 负责 rationale 和 evidence summary（解释路径）。
- 当 RAG 与 Quant 结论冲突时，以 Quant 风控约束为硬门槛，RAG 仅做备注与人工复核提示。

---

本设计文档可直接作为迭代开发蓝图：先完成 MVP 数据-因子-回测-API 闭环，再逐步增强优化与监控能力，实现“可计算、可验证、可运营”的 Quant MCP 体系。
