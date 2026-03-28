# 炒股软件 + 多Agent系统 RAG 可行性报告

> 适用仓库：SimplerJiangAiAgent（backend/frontend/desktop）  
> 日期：2026-03-28  
> 目标：在不破坏现有多Agent与交易流程的前提下，落地可解释、可控、可评估的 RAG（Retrieval-Augmented Generation）能力。

## 目录

- [1. 结论先行（Executive Summary）](#1-结论先行executive-summary)
- [2. 为什么要做 RAG：当前系统痛点](#2-为什么要做-rag当前系统痛点)
- [3. 现有数据资产盘点与优先级分层](#3-现有数据资产盘点与优先级分层)
- [4. 可用 RAG 方案（7 种）对比](#4-可用-rag-方案7-种对比)
- [5. 推荐路线图（2周 MVP / 4-6周增强 / 长期）](#5-推荐路线图2周-mvp--4-6周增强--长期)
- [6. 关键技术选型建议](#6-关键技术选型建议)
- [7. 本仓库可落地 API 接入点](#7-本仓库可落地-api-接入点)
- [8. 评估指标与验收门槛](#8-评估指标与验收门槛)
- [9. 失败兜底与风控](#9-失败兜底与风控)
- [10. 实施任务清单（可直接拆分开发）](#10-实施任务清单可直接拆分开发)
- [11. 里程碑风险与决策点](#11-里程碑风险与决策点)

## 1. 结论先行（Executive Summary）

1. 这个仓库已经具备 RAG 最关键的三件事：
   - 稳定的结构化业务数据（交易计划、研究会话、新闻本地库）。
   - 多条可复用 API 链路（`/api/stocks/research/*`、`/api/stocks/plans*`、`/api/news*`、`/api/stocks/news/impact`）。
   - 多Agent调用入口（Copilot/Research pipeline + MCP 工具端点）。
2. 最低风险、最快落地路线：
   - **阶段1（2周）**：先做“时间窗过滤 + BM25/关键词检索 + 可追溯证据拼接”的 Lite RAG，不上复杂向量库。
   - **阶段2（4-6周）**：引入向量检索与重排（reranker），形成 Hybrid Retrieval（向量 + 关键词 + 时间衰减）。
   - **阶段3（长期）**：面向多Agent做 GraphRAG/事件图谱和策略回测闭环。
3. 预估收益：
   - 降低“新闻过期导致建议偏差”和“Agent回答证据不一致”两类核心问题。
   - 提升可解释性（回答附证据）和工程可控性（可观测指标 + 风控闸门）。

---

## 2. 为什么要做 RAG：当前系统痛点

结合当前代码结构（StocksModule + ResearchSession + LocalFact + TradingPlan），痛点不是“能不能生成回答”，而是“回答是否有最新且可信证据”。

| 痛点 | 在当前仓库中的表现 | 业务后果 |
|---|---|---|
| 证据源分散 | 新闻在 `/api/news`，影响评估在 `/api/stocks/news/impact`，研究上下文在 `/api/stocks/research/*`，计划在 `/api/stocks/plans*` | Agent 输出上下文不统一、引用遗漏 |
| 时效性不稳 | 市场类数据和新闻有强时间敏感性，旧内容可能混入当前问答 | 给出“过期正确、当下错误”的建议 |
| 可解释性不足 | 现有回答虽有多Agent结果，但证据回溯颗粒度不足（缺统一 retrieval trace） | 用户难以信任，审计难 |
| 多Agent一致性问题 | 不同 agent 可能基于不同片段得出冲突结论 | “同一问题多答案”体验差 |
| 成本不稳定 | 全量依赖 LLM 思考成本高；无检索命中控制会推高 token 与延迟 | 高峰期响应慢、费用不可控 |

**RAG 的直接价值**：把“先检索证据，再生成结论”固化成系统能力，而不是依赖 prompt 侥幸命中。

---

## 3. 现有数据资产盘点与优先级分层

### 3.1 P0（MVP 必做）

| 数据域 | 现有来源 | 推荐用途 | 入库/索引策略 |
|---|---|---|---|
| 本地新闻事实（stock/sector/market） | `/api/news`、`/api/news/archive`（LocalFact ingestion/query） | 回答“最近发生了什么、影响方向” | 按 symbol + level + publishedAt 建倒排与时间索引 |
| 研究会话与轮次 | `/api/stocks/research/active-session`、`/api/stocks/research/sessions`、`/api/stocks/research/turns` | 追溯“之前为什么这样判断” | 按 sessionId/turnId + symbol + createdAt 建索引，支持会话内检索 |
| 交易计划与告警事件 | `/api/stocks/plans*`、`/api/stocks/plans/alerts` | 将策略假设与风险触发纳入上下文 | planId/symbol/status/occurredAt 组合索引，保留变更轨迹 |

### 3.2 P1（增强期）

| 数据域 | 现有来源 | 推荐用途 | 备注 |
|---|---|---|---|
| 技术面序列（K线/分时/signals） | `/api/stocks/kline`、`/api/stocks/minute`、`/api/stocks/signals` | 回答“当前形态是否支持观点” | 不建议全文向量化，建议提炼成结构化特征 chunk |
| 新闻影响结果 | `/api/stocks/news/impact` | 给结论增加“方向 + 置信度”证据块 | 与原始新闻分开存，避免循环引用 |
| MCP 聚合数据 | `/api/stocks/mcp/*` | 弥补本地事实不足时的外部证据 | 必须带来源与时间戳，默认低优先级 |

### 3.3 P2（长期）

| 数据域 | 现有来源 | 用途 |
|---|---|---|
| 多Agent中间推理摘要 | copilot/research pipeline 的 agent 输出 | 做“冲突检测 + 观点演化图” |
| 回测与执行结果（需新增） | 计划执行后收益、回撤、命中日志 | 训练/评估“建议质量 proxy” |

---

## 4. 可用 RAG 方案（7 种）对比

### 4.1 方案A：本地关键词检索（BM25/FTS）+ 时间窗过滤（Lite RAG）

**架构**
1. ingestion：将新闻、研究轮次、计划事件落地到统一 `RagDocuments`（或并行只读视图）。
2. retrieval：先按 symbol/level/time window 过滤，再做 BM25/FTS TopK。
3. generation：将 TopK 证据拼接到 prompt，输出必须附 citation。

**优点**：最快、成本最低、易调试。  
**缺点**：语义泛化弱，同义表达召回不稳定。  
**成本**：低（无 embedding 费用）。  
**开发复杂度**：低。  
**风险**：对问法变化敏感。

### 4.2 方案B：本地向量库（Qdrant/pgvector）

**架构**
- 增量 embedding 管道（事件触发或轮询） -> 向量库 -> TopK semantic retrieval。
- 可选 metadata filter：symbol、level、publishedAt、sourceTier。

**优点**：语义召回明显提升。  
**缺点**：要引入 embedding 生命周期管理（重建、漂移、版本）。  
**成本**：中（CPU/GPU + 存储）。  
**复杂度**：中。  
**风险**：向量质量与分块策略不当会导致“看似相关但不可用”。

### 4.3 方案C：云端 Embedding + 托管向量检索（Azure/OpenAI + AI Search）

**架构**
- 后端异步调用云 embedding。
- 索引在托管检索服务，查询返回语义 TopK + filters。

**优点**：运维压力低、扩展快。  
**缺点**：外部依赖强、网络与费用敏感。  
**成本**：中高（按 token/请求计费）。  
**复杂度**：中。  
**风险**：合规与数据外发边界。

### 4.4 方案D：本地+云混合（Hot local, Cold cloud）

**架构**
- 72h 热数据（market/stock/sector）放本地库。
- 超出热窗或召回不足时 fallback 到云向量检索。

**优点**：兼顾实时性、成本、覆盖面。  
**缺点**：链路更复杂，需要清晰路由策略。  
**成本**：中。  
**复杂度**：中高。  
**风险**：双索引一致性和排障复杂度上升。

### 4.5 方案E：Hybrid Retrieval（关键词 + 向量 + 重排）

**架构**
1. lexical retrieval（BM25）召回 TopN。
2. vector retrieval（embedding）召回 TopN。
3. union 去重后使用 reranker（cross-encoder）排序。
4. 时间衰减打分：$score = \alpha\cdot relevance + \beta\cdot recency + \gamma\cdot source\_quality$。

**优点**：综合效果通常最好，抗问法变化能力强。  
**缺点**：工程和在线延迟略升。  
**成本**：中高。  
**复杂度**：高。  
**风险**：多阶段调参成本高。

### 4.6 方案F：GraphRAG（事件-主体-观点图）

**架构**
- 实体（symbol/sector/company/agent）与关系（影响、支持、反驳、触发）建图。
- 查询时先走子图检索，再补文本证据。

**优点**：适合多Agent冲突解释与因果链展示。  
**缺点**：建模重、前期收益慢。  
**成本**：中高。  
**复杂度**：高。  
**风险**：图谱构建质量决定上限。

### 4.7 方案G：事件驱动增量索引 + 时间衰减检索

**架构**
- 当 `/api/news` 新增事实、`/research/turns` 新增轮次、`/plans/alerts` 新增事件时触发增量索引。
- 查询侧统一 recency-aware 打分与过期剔除。

**优点**：非常贴合交易场景的时效性。  
**缺点**：需要稳健事件总线/任务调度。  
**成本**：中。  
**复杂度**：中。  
**风险**：漏索引或重复索引导致召回异常。

### 4.8 方案总览对比

| 方案 | 召回质量 | 时效性 | 成本 | 开发复杂度 | 推荐阶段 |
|---|---:|---:|---:|---:|---|
| A Lite RAG | 2/5 | 4/5 | 1/5 | 1/5 | MVP 立即上 |
| B 本地向量 | 4/5 | 4/5 | 3/5 | 3/5 | 增强期 |
| C 云托管向量 | 4/5 | 3/5 | 4/5 | 3/5 | 增强期可选 |
| D 本地+云混合 | 4/5 | 5/5 | 3/5 | 4/5 | 增强期优先 |
| E Hybrid + Rerank | 5/5 | 4/5 | 4/5 | 5/5 | 增强后半段 |
| F GraphRAG | 4/5 | 3/5 | 4/5 | 5/5 | 长期 |
| G 事件驱动增量 | 4/5 | 5/5 | 3/5 | 3/5 | MVP末期到增强期 |

---

## 5. 推荐路线图（2周 MVP / 4-6周增强 / 长期）

### 5.1 2周 MVP（可上线内测）

**目标**：先把“证据可追溯 + 时效可控”做出来。

1. 统一检索文档模型（新闻/研究/计划事件）。
2. 做 Lite RAG：symbol + time window + BM25/FTS TopK。
3. 回答强制 citation：每条结论必须附来源、时间、docId。
4. 在 `/api/stocks/research/turns` 流程中插入 retrieval step（feature flag）。
5. 增加风控闸门：证据不足时降级为“信息不足/中性建议”。

**MVP 验收线**
- P95 额外检索延迟 < 250ms。
- 关键问答 Top3 命中率 >= 70%。
- 100% 回答具备可追溯证据。

### 5.2 4-6周增强

1. 引入向量检索（本地优先）与混合召回。
2. 加 reranker，提升 Top1 相关性与可读性。
3. 建立事件驱动增量索引（news/research/plan events）。
4. 建立在线监控面板：命中率、延迟、降级率、过期证据率。

### 5.3 长期（>6周）

1. 做 GraphRAG（主体-事件-观点-结果图）。
2. 引入回测联动，将回答质量与收益 proxy 关联评估。
3. 多Agent一致性治理：冲突自动检测、分歧解释模板化。

---

## 6. 关键技术选型建议

### 6.1 向量库

| 选型 | 结论 | 理由 |
|---|---|---|
| Qdrant（本地/私有部署） | 优先推荐 | 与 .NET 集成成熟，metadata filter 强，运维可控 |
| pgvector（若已有 Postgres） | 次选 | SQL 生态友好，但当前仓库主链路非 Postgres |
| Azure AI Search | 云备选 | 上线快，但成本与外部依赖较高 |

### 6.2 Embedding 模型

| 场景 | 建议 |
|---|---|
| MVP | 先不强依赖 embedding，先 Lite RAG |
| 增强 | 中文+英文混合语料建议 bge-m3 / text-embedding-3-large（二选一，按成本定） |
| 生产稳定性 | 模型版本固定 + embeddingVersion 字段，支持重建回滚 |

### 6.3 重排器（Reranker）

- 建议：bge-reranker 或同类 cross-encoder。
- 使用方式：仅对 Top20 做重排，控制延迟。

### 6.4 Chunk 策略

1. 新闻类：按“标题+摘要+正文片段+时间+来源”切块，目标 300-600 中文字。
2. 研究会话：按 turn 切块，并保留 stage/agent 标签。
3. 交易计划：按“计划主文 + 事件日志”双层切块。
4. chunk metadata 必须包含：`symbol`、`level`、`publishedAt`、`sourceTier`、`docType`。

### 6.5 索引策略

- 双索引：lexical index + vector index。
- 增量优先：以事件驱动写入，夜间做 compact/rebuild。
- 时间衰减：默认 72h 权重最高；7天外显著降权，除非被“长期因子”白名单命中。

---

## 7. 本仓库可落地 API 接入点

以下接口可作为 RAG ingestion/retrieval 的直接对接点：

| 模块 | 现有接口 | RAG接入方式 |
|---|---|---|
| 新闻事实 | `/api/news`、`/api/news/archive` | ingestion 主入口；按 stock/sector/market 分桶 |
| 新闻影响 | `/api/stocks/news/impact` | 作为“二级特征证据”而非原始事实 |
| 研究会话 | `/api/stocks/research/active-session`、`/api/stocks/research/sessions`、`/api/stocks/research/turns` | 会话检索与问答上下文拼接主入口 |
| 交易计划 | `/api/stocks/plans`、`/api/stocks/plans/{id}`、`/api/stocks/plans/alerts` | 交易约束、历史触发事件、风控规则上下文 |
| 多Agent草稿/门控 | `/api/stocks/copilot/turns/draft`、`/api/stocks/copilot/live-gate` | 在 draft/live-gate 前插 retrieval，输出证据引用 |
| 外部扩展证据 | `/api/stocks/mcp/*` | 作为召回不足时 fallback，默认低优先级 |

说明：前端测试中存在 `/api/stocks/agents/history` 语义路径。建议在 RAG 落地时统一“Agent历史检索”接口命名，避免 `copilot/research/agents` 多套概念并存。

---

## 8. 评估指标与验收门槛

### 8.1 离线指标（先做）

| 指标 | 定义 | 目标（MVP） |
|---|---|---|
| Hit@3 | 期望证据是否在Top3 | >= 70% |
| NDCG@5 | 排序质量 | >= 0.75 |
| Freshness Pass Rate | 证据是否在允许时间窗内 | >= 95% |
| Citation Completeness | 输出是否含来源+时间+docId | 100% |

### 8.2 在线指标（必须监控）

| 指标 | 定义 | 目标 |
|---|---|---|
| Retrieval P95 | 检索额外耗时 | < 250ms（MVP） |
| End-to-End P95 | 端到端响应 | < 2.5s（非流式） |
| Hallucination Guard Trigger Rate | 幻觉防护触发率 | 可观测且逐周下降 |
| Degrade Rate | 降级为中性/不足数据比例 | 可控，且与市场波动匹配 |

### 8.3 与收益相关的 proxy（谨慎使用）

| Proxy | 说明 |
|---|---|
| 建议一致性收益差 | 有RAG vs 无RAG 的策略模拟收益/回撤差 |
| 预警有效率 | 告警后 N 日内达到触发条件比例 |
| 失效识别速度 | 结论被反证后，系统收敛到中性所需时间 |

---

## 9. 失败兜底与风控

1. **反幻觉（Anti-hallucination）**
   - 无证据不输出强结论；证据冲突时输出“分歧态”并列出冲突点。
2. **时间窗污染防护（Temporal contamination）**
   - 默认只看 72h 热窗；超窗证据必须显式标记“历史参考”。
3. **旧信息误导防护**
   - 对价格/公告类信息启用“时间戳硬校验”；过期直接剔除。
4. **来源质量分层**
   - sourceTier（trusted/normal/unverified）影响排序权重与输出措辞。
5. **系统降级路径**
   - 向量库不可用 -> 回退关键词检索。
   - 检索不足 -> 输出中性建议 + 请求用户缩小问题范围。
6. **审计与回放**
   - 每次回答记录 retrieval trace：query、filters、topK docs、最终引用。

---

## 10. 实施任务清单（可直接拆分开发）

### 10.1 后端任务

| ID | 任务 | 产出 |
|---|---|---|
| BE-RAG-01 | 新建 RAG 文档统一模型与存储（新闻/研究/计划事件） | `RagDocument` 实体 + schema initializer |
| BE-RAG-02 | 实现 Lite retrieval service（time filter + BM25/FTS） | `IRagRetrievalService` + 单测 |
| BE-RAG-03 | 在 research turn 流程中接入 retrieval（feature flag） | `/api/stocks/research/turns` 增强 |
| BE-RAG-04 | 输出 citation 协议（docId/source/publishedAt） | API 响应结构升级 |
| BE-RAG-05 | retrieval trace 日志与监控指标埋点 | 日志 + metrics endpoint |

### 10.2 前端任务

| ID | 任务 | 产出 |
|---|---|---|
| FE-RAG-01 | 研究页显示证据卡片与引用 | 证据列表 UI + 时间标签 |
| FE-RAG-02 | 冲突证据展示（support vs contradict） | 冲突提示组件 |
| FE-RAG-03 | “数据不足/降级”状态可视化 | 风险提示与操作建议 |

### 10.3 数据与算法任务

| ID | 任务 | 产出 |
|---|---|---|
| DS-RAG-01 | 构建离线评测集（问句-标准证据） | eval dataset v1 |
| DS-RAG-02 | 定义打分脚本（Hit@k/NDCG/Freshness） | 自动评测脚本 |
| DS-RAG-03 | 增强期引入向量+重排并做 A/B | 对比报告 |

### 10.4 运维与风控任务

| ID | 任务 | 产出 |
|---|---|---|
| OPS-RAG-01 | 设置检索服务健康检查与降级策略 | health + fallback policy |
| OPS-RAG-02 | 建立告警（延迟、异常、过期证据率） | dashboard + alert rules |
| OPS-RAG-03 | 审计留存策略（trace 与脱敏） | 合规与审计文档 |

---

## 11. 里程碑风险与决策点

| 决策点 | 截止时间 | 建议 |
|---|---|---|
| 是否先上向量库 | 第1周末 | 先不上，先验证 Lite RAG 命中率 |
| 本地还是云 embedding | 第3周 | 默认本地优先，云端作为弹性补充 |
| 是否进入 GraphRAG | 第6周后 | 取决于多Agent冲突解释需求是否持续高频 |

**最终建议**：
- 路线采用 **A（Lite）-> G（增量）-> E（Hybrid+Rerank）-> F（GraphRAG）**。  
- 先赢“可解释与时效”，再追求“语义最优”。这对当前炒股 + 多Agent系统是风险最低、ROI 最高的落地顺序。
