# SimplerJiangAiAgent

最小可运行骨架：
- 后端：ASP.NET 8 Web API（模块化、便于扩展）
- 前端：Vue 3 + Vite（Tab 分页，模块隔离）
- 桌面：WinForms (.NET 8) + WebView2 容器（内嵌前端）

下一步将补充：
- 后端模块划分与基础基础设施（日志/权限/配置中心）
- 股票信息模块（爬虫来源：腾讯/新浪/百度）
- 前端 UI 的 Tab 页面骨架
- WinForms 载入前端页面的壳层

已实现（后端）：
- /api/stocks/market 大盘指数
- /api/stocks/market/cache 大盘指数（缓存）
- /api/stocks/quote 个股行情
- /api/stocks/kline 个股K线
- /api/stocks/minute 个股分时
- /api/stocks/messages 盘中消息（占位）
- /api/stocks/detail 组合详情
- /api/stocks/detail/cache 组合详情（缓存）
- /api/stocks/sync 手动触发同步
- /api/admin/login 管理员登录
- /api/admin/llm/settings/{provider} LLM 配置读取/更新（需管理员 token）
- /api/admin/llm/test/{provider} LLM 调用测试（需管理员 token）

自动化同步：
- 后台定时任务按 appsettings.json 的 StockSync 配置抓取并落库

管理员配置：
- 默认账号：admin / admin123（可在 backend/SimplerJiangAiAgent.Api/appsettings.json 的 Admin 段落中修改）
- 前端新增“LLM 设置”页签用于配置 OpenAI 以及后续 LLM provider

测试：
- 后端单元测试：dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj
- 前端单元测试：cd frontend && npm run test:unit

多 Agent 自动化：
- 入口与说明：.automation/README.md
- 脚本：.automation/scripts/run.ps1, finalize.ps1, rollback.ps1
- 任务清单：.automation/tasks.json
- 约束：Git 回滚、日志留存、Playwright MCP (Edge)
