# 给 ChatGPT-5.4 (开发人员) 的当前有效任务书

> 致 ChatGPT-5.4：
> 从当前轮开始，本文件只保留 GOAL-AGENT-001-R1 / R2 / R3 三个活跃任务，以及仍然生效的架构约束。
> 你的角色不是产品 owner，而是开发执行者；我负责指挥、拆任务、定边界、做 review。Dev1、Dev2 为并行开发人员。

---

## 当前协作模式

1. 我是指挥者：负责架构、contract、review、验收口径。
2. Dev1、Dev2 是开发执行者：按当前任务书各自完成代码、测试、报告。
3. 当前主线只围绕 GOAL-AGENT-001-R1 / R2 / R3 展开，不再把旧的 Step 4.x、图表支线、旧返工记录当作当前主开发清单重复展开。
4. 若需要追溯历史实现细节，请查 `.automation/reports/`，不要把旧回执重新堆回本文件。

---

## 当前活跃范围

1. GOAL-AGENT-001-R1：证据可追溯底座。
2. GOAL-AGENT-001-R2：Agent 职责重切与 commander 推理收口。
3. GOAL-AGENT-001-R3：回放校准闭环与验收基线。

### 当前分派

1. Dev1 当前主责：GOAL-AGENT-001-R1。
2. Dev2 当前主责：GOAL-AGENT-001-R2。
3. GOAL-AGENT-001-R3 先作为紧随其后的验收主线保留；待 R1/R2 contract 稳定后，由空闲开发者接手，或由我再二次分派。

---

## 仍然生效的全局架构约束

1. 国内 A 股事实必须坚持 Local-First：公告、个股资讯、板块资讯、大盘事实优先由本地 C# 采集和数据库查询提供；不要把事实控制权重新交回“让 LLM 自己自由联网”。
2. 当前重构的目标不是“让模型更会说”，而是“让模型少猜、少编、少抢结论”。
3. commander 只能做综合判断层，不能继续做第二个新闻生成层，也不能引入上游没有引用过的新证据。
4. 高置信度结论必须依赖可回溯 evidence object，而不是只依赖漂亮文案。
5. evidence 的外部主字段应是 URL，但 URL 不是唯一约束；必须同时有 `source`、`publishedAt`、`url`、`title`、`excerpt`、`readMode`、`readStatus`，必要时再有内部 local record key。
6. “要求阅读全文”不是默认行为；只对公告、财报、监管文件、重大合同、业绩预告、以及会直接影响交易计划失效条件的新闻触发全文抓取。
7. 盘中或 degraded path 下，系统必须保守。JSON 修复、正文缺失、上游失败、证据不可追溯、信号冲突大时，confidence 必须被系统性压低。
8. 子 Agent 必须专职化，避免每个 Agent 都输出半套方向、风控和交易条件，从而制造伪共识。
9. 确定性特征优先在代码中计算，再交给 LLM 解释，不要继续让模型直接生吞长段原始 K 线和分时数组。
10. R3 上线前，系统仍然只能算“结构化分析组件”，不能对外宣称已经具备经过真实校准的概率判断能力。

---

## GOAL-AGENT-001-R1：证据可追溯底座

状态标签：`待开发（当前第一优先级）`

### 目标

把当前“source + publishedAt + url 的弱约束 evidence”升级为真正可回溯、可审查、可降权的 evidence object，并让 commander 只采信带阅读状态的证据。

### 输入

1. `StockAgentOrchestrator.cs` 当前 stock/sector/financial/trend/commander prompt contract。
2. `llm-requests.txt` 中暴露出的伪来源、空 URL、不可验证 evidence 问题。
3. 现有 local facts 查询链路、normalizer、history 落库结构、前端 evidence 展示。

### 输出

1. 统一 evidence object contract。
2. 后端正文抓取、清洗、摘要/摘录链路。
3. `readMode` / `readStatus` 语义与降权规则。
4. commander 证据采信闸门。
5. 与现有前端展示兼容的 evidence 返回 shape。

### 核心任务

1. 统一 evidence schema：至少包含 `source`、`publishedAt`、`url`、`title`、`excerpt`、`readMode`、`readStatus`、`ingestedAt`，内部按需保留 `localFactId` 或 `sourceRecordId`。
2. 落地后端优先的 article ingest/read 链路：抓正文、去广告、去导航、截断、生成本地可复用的正文摘要或摘录。
3. 规范 `readMode`：至少区分 `local_fact`、`url_fetched`、`url_unavailable`。
4. 规范 `readStatus`：至少区分 `full_text_read`、`summary_only`、`title_only`、`metadata_only`、`fetch_failed`、`unverified`。
5. 更新 prompt / parser / normalizer / history 存储，使 5 个 Agent 的 evidence 字段一致。
6. 建立 commander 证据采信规则：没有可回溯证据对象，或证据只是 `metadata_only` / `fetch_failed` / `unverified` 时，不允许进入高置信判断。
7. 前端只做兼容展示，不得在前端自行脑补 evidence 字段。

### 约束

1. 不要把“让模型自己去网页读全文”当主链。
2. 不要把面向人的主字段重新做成 evidenceId。
3. 不要一次性把所有新闻都全文抓取；全文抓取必须有触发条件。
4. 如需改表或扩历史存储，必须保持旧记录兼容读取。

### 验收

1. evidence URL 可点击，来源与发布时间可见。
2. evidence 能区分 full text、summary、title、metadata、fetch failed 等阅读状态。
3. commander 在没有高质量 evidence 时不会继续输出高 confidence。
4. 后端定向测试覆盖 prompt/normalizer/解析/兼容读取。

### Dev1 交付边界

1. 优先改后端 evidence contract、正文抓取、normalizer、history persistence。
2. 前端只允许做 evidence 展示兼容，不要扩展成新的页面工程。

---

## GOAL-AGENT-001-R2：Agent 职责重切与 commander 收口

状态标签：`待开发（可与 R1 约定 contract 后并行）`

### 目标

让 4 个子 Agent 各做各的专业结论，减少重复推理与伪共识；同时把上下文净化、确定性特征前置计算、以及 commander 的覆盖率/冲突/降级惩罚变成系统逻辑。

### 输入

1. 当前四个子 Agent 过宽的 prompt contract。
2. `localFacts.marketReports` 中对 A 股单票造成污染的上游样本。
3. R1 固定下来的 evidence object contract。

### 输出

1. 新的 stock/sector/financial/trend prompt contract。
2. A 股场景下的 context hygiene 闸门。
3. 一组由代码先算的确定性特征。
4. commander 的 coverage/conflict/degraded-path penalty 逻辑。

### 核心任务

1. 收口职责边界：
   - `stock_news` 只做个股事件事实、催化、情绪方向、证据覆盖率。
   - `sector_news` 只做板块强弱、同类股联动、政策与资金环境。
   - `financial_analysis` 只做财务质量、估值、预期差、订单与利润结构。
   - `trend_analysis` 只做趋势状态、关键位、波动率、量价结构。
   - `commander` 才能输出方向、赔率、触发条件、失效条件、仓位建议。
2. 移除四个子 Agent 中重复的 `signals/triggers/invalidations/riskLimits` 泛化输出，或降为各自领域的专用字段。
3. 对 `marketReports` 做 A 股场景净化，隔离/降权 Seeking Alpha、CoinTelegraph、海外个股、加密资产等噪音。
4. 在代码里前置计算确定性特征，例如 freshness、coverage、conflict、trend state、ATR、估值偏离、波动、历史改判差异。
5. commander 只综合上游证据和特征，不再自由补新闻。
6. 将 coverage penalty、conflict penalty、expanded-window penalty、degraded-path penalty 做成系统逻辑，而不是让模型自由发挥。

### 约束

1. 不要在 R2 中重做 R1 的正文抓取链路。
2. 不要为追求“看起来完整”而保留旧的全员半 commander 输出结构。
3. degraded path 必须优先保守，而不是优先格式完整。

### 验收

1. 四个子 Agent 的字段明显收窄，职责边界清晰。
2. commander 的结论增量来自综合，而不是重复搬运四份半成品结论。
3. marketReports 对 A 股个股的噪音污染显著下降。
4. 异常/超时/HTML/non-JSON 情况下，最终 confidence 明显下调。

### Dev2 交付边界

1. 优先改 prompt contract、context building、feature precompute、commander penalty 逻辑。
2. 依赖 R1 的 evidence object shape，但不要自行发散修改 R1 已确认字段。

---

## GOAL-AGENT-001-R3：回放校准闭环与验收基线

状态标签：`待开发（R1/R2 后启动）`

### 目标

把系统从“格式化观点”推进到“可被真实校准和持续验收的分析系统”，建立 replay、收益对齐、命中率与 Brier score 基线。

### 输入

1. `llm-requests.txt` 历史日志。
2. `StockAgentAnalysisHistory` 与 commander 历史记录。
3. 可对齐的本地行情/收益数据。

### 输出

1. replay 样本集。
2. 1/3/5/10 日收益对齐结果。
3. 命中率、Brier score、分组胜率等校准指标。
4. 开发者可观察的验收基线与报告。

### 核心任务

1. 构建一批代表性的 replay 样本：正常样本、证据不足样本、污染样本、异常修复样本。
2. 对齐历史方向/概率/confidence 与后续 1/3/5/10 日实际收益。
3. 计算至少以下指标：命中率、Brier score、分组胜率、证据可追溯率、解析修复率、污染混入率、改判解释完整率。
4. 将指标接入开发者模式或离线报告，作为后续提示词/路由/模型改动的硬验收门槛。
5. 明确“高置信度”在历史样本上的真实表现，必要时反推阈值。

### 约束

1. R3 不是新一轮文案优化，而是验收和校准工程。
2. 没有真实收益对齐与评分，不要声称概率已经校准。
3. 校准指标必须可复跑、可留档、可比较。

### 验收

1. 系统第一次具备真实 replay 和校准闭环。
2. 后续迭代可以明确回答“这次改动让命中率/Brier score 变好了还是变差了”。
3. 开发者能通过报告或开发者模式直接看到关键基线指标。

---

## 归档说明

1. 旧的 Step 4.x、图表策略、GOAL-009、GOAL-012 详细任务书已进入归档，不再作为当前主开发清单。
2. 如确需复盘旧实现边界，请去 `.automation/reports/` 查对应报告。
3. 本文件后续只维护 GOAL-AGENT-001-R1/R2/R3 的活跃指挥信息；完成后再收缩归档。
