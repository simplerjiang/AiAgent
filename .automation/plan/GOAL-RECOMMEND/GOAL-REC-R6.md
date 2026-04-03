# GOAL-REC-R6: 全链路集成验收

> **前置**: R3 + R4 + R5 全部完成
> **交付**: 全链路端到端验证通过 + 桌面打包验证

## 任务清单

### R6-1: 后端全量单测
```
dotnet test backend\SimplerJiangAiAgent.Api.Tests\SimplerJiangAiAgent.Api.Tests.csproj
```
- 现有测试无回归（≥181 通过）
- 新增推荐系统测试全部通过

### R6-2: 前端全量单测
```
npm --prefix frontend run test:unit
```
- 现有测试无回归（≥124 通过）
- 新增推荐 UI 测试全部通过

### R6-3: 后端 API Smoke Test
手动或脚本验证:
1. POST `/api/recommend/sessions` body=`{"prompt":"今天有什么值得关注的板块？"}` → 201
2. GET `/api/recommend/sessions/{id}/events` → SSE 事件流正常推送
3. 等待 TurnCompleted → GET `/api/recommend/sessions/{id}` → 报告数据完整
4. POST `/api/recommend/sessions/{id}/follow-up` body=`{"prompt":"半导体再选几只"}` → 部分重跑
5. GET `/api/recommend/sessions` → 历史列表包含以上会话
6. GET `/api/health/websearch` → 链路状态正常

### R6-4: Browser MCP 全链路验收
CopilotBrowser MCP 执行:
1. 导航到 `http://localhost:5119/?tab=stock-recommend`
2. 点击 [新建推荐]
3. 确认进度条出现并实时更新
4. 等待完成后验证:
   - 推荐报告 Tab: 板块卡片 + 个股卡片渲染正确
   - 辩论过程 Tab: 角色发言时间线完整
   - 团队进度 Tab: 5 阶段全部 ✅
5. 在追问框输入"半导体再深入看看" → 确认部分重跑触发
6. console.error 为 0

### R6-5: 桌面打包验证
```
scripts\publish-windows-package.ps1
```
- 确认 `artifacts\windows-package\SimplerJiangAiAgent.Desktop.exe` 产出
- 启动 EXE → 切换到股票推荐 Tab → 确认 UI 可访问

### R6-6: 报告编写
- `.automation/reports/GOAL-RECOMMEND-DEV.md` 
- 英文技术摘要 + 中文用户说明
- 包含所有测试命令和结果

## 验收标准
- [x] 后端 + 前端全量测试 0 failure
- [x] API smoke test 所有端点正常
- [x] Browser MCP 全链路 5 阶段完成 + 报告渲染 + 追问可用
- [x] 桌面打包产出并可启动
- [x] console.error = 0
