# 交易计划模块重构专项计划
> GOAL-UI-NEW-001 · 独立子专项
> 生成日期：2026-03-28 | 优先级：P0

---

## 背景与动机

当前状态（已确认问题）：
- `POST /api/stocks/plans` 要求 `AnalysisHistoryId > 0`，**完全阻断手动新建路径**
- 前端 `StockTradingPlanSection.vue` 无"新建"按钮，唯一入口是 commander 草稿
- 表单 `StockTradingPlanModal.vue` 无字段分组，4 列平铺，填写效率极低
- 硬删除（无软删除/归档流），无操作撤回
- `StockTradingPlanBoard.vue` 无独立新建入口，无跨标的多计划对比
- 用户代表 Agent 审查结论：**不通过（拒绝验收）**

目标（重构后）：
- **5 秒内可手动新建一条计划**（不依赖 commander 分析历史）
- **2 步编辑完成**（直接点击计划 → 改字段 → 保存）
- **计划不会被误删**（软删除 / 取消替代硬删除）
- **每条计划有明确状态流转标识**

---

## 一、信息架构（IA）

### 1.1 状态机（Status State Machine）

```
           [手动新建 / Commander 草稿]
                      │
                   Draft (0)   -- 草稿，未提交
                      │ 确认提交
                 Pending (1)   -- 等待触发
                /            \
        价格触发              时间/条件到期
               ↓                ↓
          Triggered (2)    Invalid (3)   -- 自动失效
               ↓  \
           用户执行   用户取消
               ↓       ↓
         [Executed]  Cancelled (4)  -- 用户取消（软删除语义）
         (新增状态)
               ↓
         ReviewRequired (5)  -- 触发后需人工审核
```

**新增 `Executed` 状态（值=6）**：计划已被用户标记为"已成交/已执行"，进入最终态；与 `Cancelled`、`Invalid` 并列为终态。

### 1.2 字段层次架构（分组展示）

**组 A — 基础身份**
- `symbol` (股票代码，只读)
- `name` (计划名称，必填)
- `direction` (方向：Long / Short)
- `sourceAgent` (来源：manual / commander，手动时固定为 "manual")

**组 B — 关键价位**（表单中以价格卡展示）
- `triggerPrice` (触发价，可为空)
- `invalidPrice` (失效价)
- `stopLossPrice` (止损价)
- `takeProfitPrice` (止盈价)
- `targetPrice` (目标价)

**组 C — 分析依据**（可折叠）
- `expectedCatalyst` (预期催化)
- `invalidConditions` (失效条件)
- `riskLimits` (风险上限)
- `analysisSummary` (分析摘要)

**组 D — 个人备注**（可折叠）
- `userNote` (用户备注)

**组 E — 市场上下文**（只读展示，来自创建时快照）
- `marketStageLabelAtCreation`
- `suggestedPositionScale`
- `mainlineSectorName`

---

## 二、接口草案

### 2.1 后端变更

#### 2.1.1 `POST /api/stocks/plans` — 放开 `AnalysisHistoryId` 约束

**当前问题**：`StocksModule.cs:789`
```csharp
if (request.AnalysisHistoryId <= 0)
    return Results.BadRequest(new { message = "analysisHistoryId 无效" });
```

**改造方案**：`AnalysisHistoryId` 改为可选，当 `AnalysisHistoryId == 0` 时，设置 `SourceAgent = "manual"` 并跳过关联校验。

**新 DTO**：`TradingPlanCreateDto` 中 `AnalysisHistoryId` 改为 `long? AnalysisHistoryId`（nullable），`SourceAgent` 由后端根据 AnalysisHistoryId 是否有值自动推断。

```csharp
// 新 TradingPlanCreateDto
public sealed record TradingPlanCreateDto(
    string Symbol,
    string Name,
    string? Direction,         // 默认 Long
    decimal? TriggerPrice,
    decimal? InvalidPrice,
    decimal? StopLossPrice,
    decimal? TakeProfitPrice,
    decimal? TargetPrice,
    string? ExpectedCatalyst,
    string? InvalidConditions,
    string? RiskLimits,
    string? AnalysisSummary,
    long? AnalysisHistoryId,   // ← 由 long 改为 long?（nullable）
    string? SourceAgent,
    string? UserNote
);
```

```csharp
// 新的端点逻辑（伪代码）
group.MapPost("/plans", async (TradingPlanCreateDto request, ...) =>
{
    if (string.IsNullOrWhiteSpace(request.Symbol))
        return Results.BadRequest(new { message = "symbol 不能为空" });

    // 不再强制要求 AnalysisHistoryId > 0
    // 若无 analysisHistoryId，sourceAgent 强制为 "manual"
    var effectiveSourceAgent = (request.AnalysisHistoryId is null or <= 0)
        ? "manual"
        : (request.SourceAgent ?? "commander");

    var result = await tradingPlanService.CreateAsync(request with { SourceAgent = effectiveSourceAgent });
    ...
});
```

#### 2.1.2 `POST /api/stocks/plans/{id}/execute` — 新增"标记执行"端点

```
POST /api/stocks/plans/{id:long}/execute
响应：200 OK → TradingPlanItemDto (status=Executed)
```

这是手动工作流的最终态，与现有 `/cancel`、`/resume` 结构一致。

#### 2.1.3 `POST /api/stocks/plans/{id}/soft-delete` — 软删除（可选，优先级中）

可以用现有的 `/cancel` 替代，不强制新增端点。**优先用取消代替物理删除，前端隐藏取消态数据。**

#### 2.1.4 `TradingPlan` 实体新增字段

```csharp
// TradingPlan.cs 新增
public TradingPlanStatus Status { get; set; }  // 已有，增加 Executed = 6
public DateTime? ExecutedAt { get; set; }       // 新增
```

**Schema 迁移**：`TradingPlanSchemaInitializer.cs` 中追加：
```sql
IF COL_LENGTH('dbo.TradingPlans','ExecutedAt') IS NULL
    ALTER TABLE dbo.TradingPlans ADD ExecutedAt DATETIME2 NULL;
```

### 2.2 前端 API 调用约定

| 操作 | 方法 | URL |
|------|------|-----|
| 拉取当前股票计划列表 | GET | `/api/stocks/plans?symbol={symbol}` |
| 拉取全局计划总览 | GET | `/api/stocks/plans?take=50` |
| 手动新建计划（无分析历史） | POST | `/api/stocks/plans` with `{ analysisHistoryId: null }` |
| Commander 草稿确认入库 | POST | `/api/stocks/plans` with `{ analysisHistoryId: <id> }` |
| 编辑计划（全字段可改） | PUT | `/api/stocks/plans/{id}` |
| 取消计划（不删除，状态 → Cancelled） | POST | `/api/stocks/plans/{id}/cancel` |
| 标记执行 | POST | `/api/stocks/plans/{id}/execute` |
| 恢复（Cancelled → Pending） | POST | `/api/stocks/plans/{id}/resume` |
| 彻底删除（管理员操作，二次确认） | DELETE | `/api/stocks/plans/{id}` |

---

## 三、前後端任务拆分

### 3.1 后端任务（Backend Tasks）

| 任务 ID | 文件 | 变更描述 | 优先级 |
|---------|------|----------|--------|
| BE-TP-01 | `TradingPlanDto.cs` | `TradingPlanCreateDto.AnalysisHistoryId` 改为 `long?` | P0 |
| BE-TP-02 | `StocksModule.cs` | 移除 `AnalysisHistoryId <= 0` 的 BadRequest 门控，改为推断 `sourceAgent` | P0 |
| BE-TP-03 | `ITradingPlanService.cs` | `CreateAsync` 支持 `AnalysisHistoryId` 为 null 时跳过外键关联 | P0 |
| BE-TP-04 | `TradingPlan.cs` | 新增 `Executed = 6` 枚举值，新增 `ExecutedAt` 字段 | P1 |
| BE-TP-05 | `TradingPlanSchemaInitializer.cs` | 追加 `ExecutedAt` 列 ALTER TABLE | P1 |
| BE-TP-06 | `StocksModule.cs` | 新增 `POST /plans/{id}/execute` 端点 | P1 |
| BE-TP-07 | `ITradingPlanService.cs` | 新增 `ExecuteAsync(long id)` 方法 | P1 |
| BE-TP-08 | `TradingPlanTriggerWorker.cs` | 确认终态（Executed/Cancelled/Invalid）不触发自动检测 | P2 |
| BE-TP-09 | Tests | 新增手动创建（无 analysisHistoryId）的单元测试 | P0 |
| BE-TP-10 | Tests | 新增 `Execute` 状态流转测试 | P1 |

### 3.2 前端任务（Frontend Tasks）

| 任务 ID | 文件 | 变更描述 | 优先级 |
|---------|------|----------|--------|
| FE-TP-01 | `StockTradingPlanSection.vue` | 全重写：增加"新建计划"按钮，移除 commander 依赖 | P0 |
| FE-TP-02 | `StockTradingPlanModal.vue` | 全重写：字段分 4 组，折叠/展开，价格卡布局 | P0 |
| FE-TP-03 | `stockInfoTabTradingPlans.js` | 新增 `createManualPlan(symbol)`、`executePlan(id)`、`cancelPlan(id)` | P0 |
| FE-TP-04 | `StockTradingPlanBoard.vue` | 增加"新建计划"快捷入口，支持跨标的滚动列表 | P0 |
| FE-TP-05 | 新建 `TradingPlanCreateDrawer.vue` | 独立的"手动新建"抽屉组件（Drawer 方案，宽度 480px） | P0 |
| FE-TP-06 | 新建 `TradingPlanEditDrawer.vue` | 独立的"编辑"抽屉组件（可与 Create Drawer 合并为一个） | P0 |
| FE-TP-07 | `stockInfoTabTradingPlans.js` | 状态显示增加 `Executed`，图标/颜色对应 | P1 |
| FE-TP-08 | `StockTradingPlanSection.vue` | "取消"替代"删除"（终态显示为已取消但列表保留可见） | P1 |
| FE-TP-09 | 新建 `TradingPlanWorkbench.spec.js` | 新建 + 编辑 + 状态流转 的 Vitest 测试 | P0 |
| FE-TP-10 | 修复 `StockInfoTab.vue` | 更新 `StockTradingPlanSection` 的 emit 绑定（增加 `create`、`execute` 事件） | P0 |

### 3.3 组件树（重构后）

```
StockInfoTab.vue
└── StockTradingPlanSection.vue        ← 重写（保留组件名，Props 接口向后兼容）
    ├── [header] 当前交易计划 | [+ 新建计划] button
    ├── [plan list]
    │   └── PlanItem                   ← 每条计划：展开查看详情 + 编辑 + 取消
    └── TradingPlanDrawer.vue          ← 新建 / 编辑 共用 Drawer（新建组件）
        ├── Group A: 基础身份
        ├── Group B: 关键价位（价格卡）
        ├── Group C: 分析依据（可折叠）
        └── Group D: 个人备注（可折叠）
```

---

## 四、测试用例矩阵

### 4.1 后端单元测试

| 测试 ID | 场景 | 预期结果 | 标注 |
|---------|------|----------|------|
| TP-BE-01 | `CreateAsync(analysisHistoryId: null)` | 计划创建成功，`SourceAgent = "manual"` | P0 |
| TP-BE-02 | `CreateAsync(analysisHistoryId: 999)` (不存在的 ID) | 创建成功但外键不关联（AnalysisHistory 为 null） | P0 |
| TP-BE-03 | `CreateAsync(symbol: "")` | BadRequest | P0 |
| TP-BE-04 | `UpdateAsync` 修改 triggerPrice | 返回更新后的 DTO，`UpdatedAt` 变化 | P0 |
| TP-BE-05 | `CancelAsync` 后再 `CancelAsync` | 第二次返回 BadRequest（已取消） | P1 |
| TP-BE-06 | `ResumeAsync` 从 Cancelled → Pending | 状态正确流转 | P0 |
| TP-BE-07 | `ExecuteAsync` 从 Triggered → Executed | 状态正确，`ExecutedAt` 有值 | P1 |
| TP-BE-08 | `ExecuteAsync` 从 Pending (未触发) → Executed | 允许（手动标记已执行） | P1 |
| TP-BE-09 | `POST /plans` HTTP 端点，无 `analysisHistoryId` 字段 | 200 OK | P0 |
| TP-BE-10 | `POST /plans/{id}/execute` HTTP 端点 | 200 OK，DTO status = "Executed" | P1 |

### 4.2 前端单元测试（Vitest）

| 测试 ID | 场景 | 断言 |
|---------|------|------|
| TP-FE-01 | `StockTradingPlanSection` 渲染时，存在"新建计划"按钮 | `wrapper.find('[data-testid="create-plan-btn"]')` 存在 |
| TP-FE-02 | 点击"新建计划"后，`TradingPlanDrawer` 显示 | `drawerVisible === true` |
| TP-FE-03 | Drawer 中 symbol 字段为只读 | `input[name=symbol].disabled === true` |
| TP-FE-04 | "组 B 价格卡"所有 price 字段均可输入 | 5 个 input 可聚焦 |
| TP-FE-05 | 点击"保存"调用 `POST /api/stocks/plans` 且 body 无 `analysisHistoryId`（或为 null） | fetch mock 被调用，body 匹配 |
| TP-FE-06 | 保存成功后 Drawer 关闭，列表刷新 | `drawerVisible === false`，`emit('refresh')` 被触发 |
| TP-FE-07 | 计划状态为 Triggered，显示"标记执行"按钮 | button 存在 |
| TP-FE-08 | 计划状态为 Cancelled/Invalid/Executed，不显示编辑按钮 | 编辑 button 不渲染 |
| TP-FE-09 | `formatTradingPlanStatus("Executed")` 返回 `"已执行"` | 字符串匹配 |
| TP-FE-10 | 并发保存测试：连续点 2 次保存，仅调用 1 次 API | fetch 被调用 1 次（loading guard） |

### 4.3 Browser MCP 验收测试（手工流程 / CopilotBrowser）

| 步骤 ID | 操作 | 预期可见结果 |
|---------|------|-------------|
| BT-01 | 打开股票信息页，选任意股票 | 页面底部"当前交易计划"区域显示"+ 新建计划"按钮 |
| BT-02 | 点击"+ 新建计划" | 右侧 Drawer 滑出，symbol 已预填 |
| BT-03 | 只填写"计划名称"和"方向"，点击保存 | 计划入库，Drawer 关闭，列表出现新记录 |
| BT-04 | 点击已有计划的编辑 | Drawer 重新打开，字段预填 |
| BT-05 | 修改 triggerPrice，保存 | 列表中价格更新 |
| BT-06 | 点击计划的"取消" | 计划状态变为"已取消"，仍在列表中可见（灰色） |
| BT-07 | 点击已触发计划的"标记执行" | 状态变为"已执行"，绿色徽章 |
| BT-08 | 打开"交易计划总览"（Board） | 可见全部计划，含新建的手动计划 |
| BT-09 | 在 Board 中点击"+ 新建计划" | 相同 Drawer 弹出，symbol 字段为空（待输入） |
| BT-10 | 控制台检查 | 无 `Error` 级别报错 |

---

## 五、边界与例外处理

| 场景 | 处理方案 |
|------|----------|
| 用户不填 triggerPrice 等价格 | 允许为空，后端不校验（已有 `decimal?`） |
| symbol 搜索结果空（在 Board 新建时） | Drawer 内嵌 symbol 搜索输入框（复用 StockSearchToolbar 逻辑） |
| 并发编辑（两标签同时改同一计划） | 前端 `loading` guard + 后端 EF `SaveChangesAsync` 乐观并发（默认行为） |
| Commander 生成草稿 → 用户手动再新建同一支股票 | 允许，计划列表支持同一股票多条 Pending |
| Executed/Cancelled/Invalid 状态被误操作 | 隐藏"编辑"按钮，仅展示"查看详情" |

---

## 六、接受指标（Definition of Done）

| 指标 | 标准 |
|------|------|
| 手动新建路径 | `POST /plans` 无需 `analysisHistoryId` 即可成功（HTTP 200） |
| 编辑路径 | `PUT /plans/{id}` 任意字段均可修改 |
| 状态流转完整 | Pending→Triggered→Executed / Pending→Cancelled 均可触达 |
| 前端新建耗时 | 从点击"+ 新建"到计划出现在列表 < 3 秒（正常网络） |
| 无误删 | 删除按钮改为"取消"，原 DELETE 仅管理员可触达 |
| 后端测试 | TP-BE-01～09 全部通过 |
| 前端测试 | TP-FE-01～10 全部通过 |
| Browser MCP | BT-01～BT-10 全部通过 |
| 旧功能不破坏 | Commander 草稿→确认入库路径仍可用（BT 回归验证） |

---

## 七、执行日志

| 日期 | 操作 | 执行者 | 结果 |
|------|------|--------|------|
| 2026-03-28 | 产出交易计划重构专项计划 | PM Agent | ✅ 完成 |
