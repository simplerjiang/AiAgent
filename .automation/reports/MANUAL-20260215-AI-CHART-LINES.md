# MANUAL-20260215-AI-CHART-LINES

## Planning Report (EN)
### Scope
- Implement AI-derived breakout/resistance and support lines on both K-line and minute charts.
- Keep implementation in existing modules only (`StockInfoTab.vue`, `StockCharts.vue`) without creating new modules.

### Plan
1. Extract structured numeric levels from existing multi-agent results.
2. Pass levels into existing chart component via props.
3. Add overlay line series for K-line and minute charts.
4. Add/adjust unit tests for each newly introduced line series.
5. Run unit tests first, then run Edge interaction check.
6. Sync `README.md`, `.automation/tasks.json`, `.automation/state.json`, and continuous rules.

## 计划报告（ZH）
### 范围
- 在 K 线和分时图中增加 AI 推导的突破/支撑线。
- 仅在现有模块内实现（`StockInfoTab.vue`、`StockCharts.vue`），不新增模块。

### 计划
1. 从现有多Agent结果中提取结构化数值价位。
2. 通过属性传入现有图表组件。
3. 在 K 线与分时图增加叠加线序列。
4. 为新增序列补充/更新单元测试。
5. 严格按“先单测、后 Edge 交互检查”执行验证。
6. 同步 `README.md`、`.automation/tasks.json`、`.automation/state.json` 与持续规则。

## Development Report (EN)
### Files Changed
- `frontend/src/modules/stocks/StockCharts.vue`
  - Added `aiLevels` prop.
  - Added resistance/support line series for K-line and minute charts.
  - Added safe numeric parsing and horizontal-line data builders.
  - Added lightweight AI line text hint for visual confirmation.
  - Extended reactive render watch inputs to include `basePrice` and `aiLevels`.
- `frontend/src/modules/stocks/StockInfoTab.vue`
  - Added AI level extraction from existing `agentResults`.
  - Precedence: commander recommendation (`target/takeProfit`, `stopLoss`) first; fallback to trend forecast max/min.
  - Passed computed `aiLevels` into `StockCharts`.
- `frontend/src/modules/stocks/StockCharts.spec.js`
  - Extended mocks for newly introduced line series.
  - Added assertions for minute and K-line resistance/support data mapping.
- `README.md`
  - Updated implemented frontend feature list and GOAL-007 detail for AI chart overlays.
- `.automation/tasks.json`
  - Updated GOAL-007 notes with chart overlay completion details.
- `.automation/state.json`
  - Updated `currentRun.reportPath` to this report.
- `.github/copilot-instructions.md`
  - Added one new continuous rule (EN + ZH) for numeric-only AI overlay rendering and deterministic fallback order.

### Validation
- Unit test command:
  - `cd frontend && npm run test:unit`
- Result:
  - Passed: 6 test files, 23 tests.
  - Includes updated `StockCharts.spec.js` coverage for new line series.
- Edge interaction command:
  - `cd frontend && node scripts/edge-check-goal007.mjs`
- Edge result:
  - success=`true`, hasStockName=`true`, hasAgentCard=`true`, hasRawJson=`true`, chatInteractive=`true`, consoleErrors=`[]`.
- Backend log check command:
  - `Get-Content backend/SimplerJiangAiAgent.Api/App_Data/logs/llm-requests.txt -Tail 120 | Select-String -Pattern "error|exception|fail" -SimpleMatch`
- Backend log result:
  - No matched runtime error keywords in latest inspected log window.

### Issues / Notes
- No backend code changes were required for this task.
- Edge interaction check is still required by workflow and should be run after unit tests.

## 开发报告（ZH）
### 修改文件
- `frontend/src/modules/stocks/StockCharts.vue`
  - 新增 `aiLevels` 属性。
  - 为 K 线与分时图新增突破/支撑线叠加序列。
  - 增加数值安全解析与水平线数据构建逻辑。
  - 增加 AI 线提示文案，便于可视验证。
  - 扩展响应式监听，纳入 `basePrice` 与 `aiLevels`。
- `frontend/src/modules/stocks/StockInfoTab.vue`
  - 从现有 `agentResults` 提取 AI 关键价位。
  - 优先级：先 commander 建议（目标/止盈、止损），缺失再回退 trend 预测区间极值。
  - 将 `aiLevels` 传入 `StockCharts`。
- `frontend/src/modules/stocks/StockCharts.spec.js`
  - 扩展新增序列 mock。
  - 新增分时/K线突破与支撑线的数据映射断言。
- `README.md`
  - 更新前端已实现功能与 GOAL-007 细节（图表叠加线）。
- `.automation/tasks.json`
  - 更新 GOAL-007 备注，记录图表叠加线完成情况。
- `.automation/state.json`
  - 将 `currentRun.reportPath` 同步到本报告。
- `.github/copilot-instructions.md`
  - 新增一条持续规则（中英双语）：AI 图线仅渲染数值并采用确定性回退优先级。

### 验证
- 单元测试命令：
  - `cd frontend && npm run test:unit`
- 结果：
  - 6 个测试文件全部通过，23 个测试全部通过。
  - 已覆盖 `StockCharts.spec.js` 中新增叠加线逻辑。
- Edge 交互命令：
  - `cd frontend && node scripts/edge-check-goal007.mjs`
- Edge 结果：
  - success=`true`，hasStockName=`true`，hasAgentCard=`true`，hasRawJson=`true`，chatInteractive=`true`，consoleErrors=`[]`。
- 后端日志检查命令：
  - `Get-Content backend/SimplerJiangAiAgent.Api/App_Data/logs/llm-requests.txt -Tail 120 | Select-String -Pattern "error|exception|fail" -SimpleMatch`
- 后端日志结果：
  - 最近检查窗口未匹配到运行时错误关键字。

### 问题与说明
- 本次未改动后端代码，无需后端测试。
- 按流程仍需在单测后执行 Edge 交互检查。
