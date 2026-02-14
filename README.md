# SimplerJiangAiAgent

面向真实交易决策的智能化炒股助手。目标是把“行情 + 事件 + 指标 + 研报 + 资金面 + 你的交易习惯”整合到一个可追踪、可解释、可持续演进的系统里，输出更可靠的交易辅助判断。

## 项目架构
- 后端：ASP.NET 8 Web API（模块化、可扩展）
- 前端：Vue 3 + Vite（Tab 分页，模块隔离）
- 桌面：WinForms (.NET 8) + WebView2（内嵌前端）

## 已实现功能
### 后端
- /api/stocks/market 大盘指数
- /api/stocks/market/cache 大盘指数（缓存）
- /api/stocks/quote 个股行情
- /api/stocks/kline 个股K线
- /api/stocks/minute 个股分时
- /api/stocks/messages 盘中消息（占位）
- /api/stocks/detail 组合详情
- /api/stocks/detail/cache 组合详情（缓存）
- /api/stocks/sync 手动触发同步
- /api/stocks/news/impact 资讯影响评估（公告/研报/新闻分级、来源可信度、同主题合并去重）
- /api/stocks/signals 事件驱动信号（证据/反证、历史对齐）
- /api/stocks/position-guidance 个性化风险与仓位建议
- /api/admin/login 管理员登录
- /api/admin/llm/settings/{provider} LLM 配置读取/更新（需管理员 token）
- /api/admin/llm/test/{provider} LLM 调用测试（需管理员 token）

### 前端
- Stock Tabs 基础框架
- LLM 设置页签（管理员配置）

### 桌面端
- WinForms 容器 + WebView2 载入前端

## 数据同步与配置
- 后台定时任务按 appsettings.json 的 StockSync 配置抓取并落库
- 默认账号：admin / admin123（可在 backend/SimplerJiangAiAgent.Api/appsettings.json 的 Admin 段落中修改）

## 测试
- 后端单元测试：dotnet test backend/SimplerJiangAiAgent.Api.Tests/SimplerJiangAiAgent.Api.Tests.csproj
- 前端单元测试：cd frontend && npm run test:unit

## OpenCode（VS Code + CLI）接入
本仓库已提供 OpenCode 项目级配置文件：`opencode.json` 与 `AGENTS.md`。

### 已配置内容
- 使用 OpenAI 兼容 provider 方式接入 GLM-5
- 项目指令文件：`.github/copilot-instructions.md` + `AGENTS.md`
- 默认模型：`zai/glm-5`

### 环境变量（必须）
请在终端设置你的 GLM-5 key（不要写入仓库）：

PowerShell:

```powershell
$env:GLM_API_KEY="你的GLM-5_API_KEY"
```

如果需要长期生效，请在你的用户环境变量里配置 `GLM_API_KEY`。

### 启动方式
在项目根目录运行：

```powershell
opencode
```

首次建议执行：
- `/models` 确认已选中 `zai/glm-5`
- `/init` 让 OpenCode 读取仓库结构与规则

## 多 Agent 自动化开发与测试
入口与说明： [.automation/README.md](.automation/README.md)

核心目标：让自动化“长时间持续改进”更可靠、更可回滚、更可审计。设计原则参考长时间代理系统的一般实践：
- 任务切分清晰，避免超大变更
- 每次运行有清晰的计划、变更、测试和日志
- 可回滚的 git checkpoint，失败立即止损
- 强制执行测试顺序并记录结果

## 当前目标清单（与 .automation/tasks.json 同步）
- [x] GOAL-002 多源事件与权重评估（公告/研报/新闻分级、来源可信度、同主题去重）
- [x] GOAL-003 事件驱动信号与可解释输出（证据/反证、历史对齐）
- [x] GOAL-004 个性化风控与仓位建议
- [x] GOAL-005 专业行情图升级（K线+成交量副图、分时专业渲染、数据精确映射）
- [x] GOAL-006 图表增强（二期：分时成交量副图 + K线 MA5/MA10 叠加线）

## 未来目标（智能化炒股助手愿景）
以下目标不是口号，而是系统可以逐步落地的路线图：

### 更全面的数据与事件理解
- 多源行情与盘口：深度盘口、逐笔成交、资金流向、筹码分布
- 多源新闻与公告：公告、研报、新闻、社媒情绪、主题热度
- 产业链图谱：上下游关联、同主题联动、资金共振

### 更强的分析与解释能力
- 量化指标集成：多时间尺度趋势、波动、拐点、背离检测
- 事件驱动分析：公告影响评估、预期差建模
- 可解释信号：每个建议都给出依据与反证

### 更贴近个人交易风格
- 个人偏好与风险画像
- 持仓与策略协同：仓位、止损、止盈、风控联动
- 交易复盘与反馈：用复盘训练个人策略偏好

### 更接近“真实交易”的辅助系统
- 策略沙盒与回测：支持多策略并行评估
- 预警系统：关键价位、资金异动、情绪突变
- 组合层面辅助：行业分散、风险暴露、相关性控制

### 安全与合规
- 默认只做决策辅助，不自动下单
- 关键建议需二次确认
- 记录每次建议的证据与来源
