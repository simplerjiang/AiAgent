# 📋 给 ChatGPT-5.4 (开发人员) 的任务书与架构纠偏指令

> **致 ChatGPT-5.4**: 
> 你好，我是这个项目的系统产品经理和架构监督者（Copilot Agent）。用户授权我负责本项目的需求转化与质量检查。在此前的发展中，我发现系统的部分功能和技术栈已经偏离了 `README.md` 的初衷。
> 
> 在接下来的开发中，你作为**一线开发人员**，必须**严格听从**本文件记录的指令，不得自作主张。

---

## 🚨 架构审查发现的问题与纠偏 (旧 GOAL 诊断)

经过我对 `tasks.json` 中已完成的过历史目标（如 GOAL-002/003/007）和后续计划的彻底回溯分析，目前项目犯了两个致命方向错误：

1. **图表与AI功能高度耦合 (违反了最新的 GOAL-012 取向):**
   * **现象**: 前端目前的实现将图表交互和频繁弹出的诊断日志混合呈现，导致不仅看盘视线受干扰，且运行时性能受到挑战。
   * **纠偏**: 原有的界面布局必须推倒重组为 **Grid（网格屏）布局**。左侧 `70%+` 的绝对空间只保留 `lightweight-charts` 和数据挂件；AI Copilot (例如你生成的对话和建议) **一律放入右侧 / 侧边 Drawer**，只有用户显式需要时才通过交互面板查阅。绝不能让 AI 弹窗阻挡 K 线。

2. **过度依赖大语言模型抓取动态数据 (违反了最新的 GOAL-013 本地优先理念):**
   * **现象**: 旧的 P1~P3 计划或以往的 Agent 实现中，系统倾向通过给 LLM 注入外部 Search 权限或 MCP 插件实时向外网请求各种新闻。这不仅慢、且在国内A股等确定性事实上容易产生幻觉污染。
   * **纠偏 (双轨制数据策略)**: 建立内外有别的“双轨数据流”。对于**中国A股公告、预告、国内财经资讯**，必须由 C# 的 `SimplerJiangAiAgent.Api` 编写稳定的 Background Service 定时去东方财富/同花顺抓取以结构化存入本地 SQL 库，限制 LLM 对此部分数据的直接外网查询；而对于**海外宏观、美股映射资金、国际政经新闻**，由于无法固定抓取池，需为 AI 继续保留搜索工具的动态外网权限，但要求 LLM 进行严格场景判断后方可使用。

---

## 🛠️ 下一步开发执行路线 (Sprint Tasks)

请你按照以下 **三步走** 的顺序展开代码工作。每完成一个步骤并自测后，请停下来，让用户来找我 (Reviewer 角色) 验收，**不要连着开发多步**。

### Step 1: 客户端 UI 骨架拆分重构 (攻克 GOAL-012)
* **任务**: 编辑前端 `frontend/src` 中的核心视图（如 `App.vue` / 主布局系统页面）。
* **要求**:
  * 建立基于 CSS Grid / Flex 的清晰排版体系。
  * 将 `K线图`、`分时图`、`成交量和均线` 限定在一个名叫 `TerminalView` 的独立核心区。
  * 将 `AI对话`、`事件信号` 等组件收拢到一个名叫 `CopilotPanel` 的侧边抽屉或右置边栏。
  * 确保在不唤醒 AI 服务时不发任何 LLM 请求，提供原生看盘软件般流畅的体验。

### Step 2: C# 本地数据中枢基建建立 (推进 GOAL-013)
* **任务**: 在后端 `SimplerJiangAiAgent.Api/Infrastructure` 下，新增传统的本地数据采集层。
* **要求**:
  * 设计数据库表结构（如 `LocalMarketNews`, `LocalSectorRotation`）。
  * 编写 `IHostedService` 或借助 Quartz/Hangfire 创建定时任务服务，负责抓取和解析。
  * 数据落库必须包含强类型时间戳、来源标签。
  * 更新 `LLM Tool` 权限，砍掉原先联网搜网页的 Prompt Tool，替换为 "QueryLocalDatabaseBaseTool" 相关的实现。

### Step 3: 重写交易事件分发系统 (整合 ISSUE-P1/P2)
* **任务**: 在有了本地干净的数据（Step 2）后，完善后端基于事实的数据分析。
* **要求**:
  * 利用本地 C# 规则引擎先屏蔽/隔离“小作文”等不可靠信息源。
  * 将干净且已入库的本地数据，以结构化 JSON 的形式喂给 LLM。
  * 让 LLM 仅负责总结并输出**情绪循环、打板概率**等投研建议。

---

> **💬 ChatGPT-5.4 验收确认规范**: 
> 开发时，请确保使用原生的 IDE Edit (例如使用 VS Code Editor 真实改代码，而非运行终端 Python 脚本改代码)，保证 Git 有记录。当你搞定 Step 1 时，直接告诉用户：“**开发已完成 Step 1，请去唤醒 Copilot 产品经理进行 Review！**”

---

## 🟢 Reviewer 验收指令: Step 1 通过 & 启动 Step 2 (2026-03-12)

> **致 ChatGPT-5.4**:
> 你的 Step 1.1 返工代码已由架构师 Review 验收通过！前端 UI 的物理隔离（看盘终端与 AI 侧栏分离）以及顶部工具栏的紧凑化彻底释放了K线图的视野，满足了 GOAL-012 的要求。
> 
> 接下来，**必须严格进入 Step 2：C# 本地数据中枢基建建立 (推进 GOAL-013) 的开发。**
>
> **你的开发任务 (Step 2 - 双轨制数据中心)**:
> 1. **数据库扩充 (内轨基石)**: 在 `backend/SimplerJiangAiAgent.Api/Data/Entities` 新增 `LocalStockNews` 和 `LocalSectorReport` 实体类，包含强字段 `Symbol`、`PublishTime` 等，注册 DbSet 并生成 Migration。
> 2. **后端采集管线 (内轨 - 严格按此规则)**: 在 Infrastructure 层实现强类型 `IHostedService`。
>    * **🚨 PM 实测排雷警告**: 我刚刚亲自测试了数据源。**同花顺(10jqka)** 的所有 ajax 接口（如 `/ajax/code/...`）带有严格的 `v` 动态 Cookie 校验，单纯的 `HttpClient` 会直接被 `403 Forbidden` 拦截。因此 **绝对禁止** 开发人员脑补和编写毫无作用的同花顺采集代码！
>    * **🟢 指定使用东方财富 (Eastmoney) API**: 东方财富的公告 API 非常干净友好，直出的 JSON 且无防爬。你必须使用如下 URL 结构进行个股信息拉取：
>      `GET https://np-anotice-stock.eastmoney.com/api/security/ann?page_size=30&page_index=1&ann_type=A&client_source=web&stock_list={Symbol}`
>      (只需解析返回的 `data.list` 数组，提取 `title`, `display_time`, `art_code` 即可，**不要乱发明不存在的 API**)。
>    * **综合使用**: 结合现有的 `SinaCompanyNewsParser` 和上面指定的东财强类型 JSON API，完善后台定时抓取任务。
> 3. **AI Search 路由逻辑 (外轨 & 调度)**: 检视现有 Agent Tools/Plugins 中直接调用外网的搜索工具。**不要停用或废弃它**，而是为其加上严格的系统 Prompt 或代码级策略控制（如：当查询请求不包含国内特定单只A股代码，或者明确含有“海外、宏观、美股映射”等标签时才允许外网查）。
> 4. **赋予受控新能力**: 为 Agent 编写新的 Tool `QueryLocalFactDatabaseTool`。引导 LLM 在处理“A股研报分析”、“股票公告查询”时强制切向本地 SQL 事实库检索。
> 5. **前端数据联动与精准过滤 (解决无关新闻占位问题)**: UI 层面已预留了新闻坑位，但目前展示的数据是全局且无关的。你需要开发/改造对应的查询 API（如 `GET /api/news?symbol={代码}&level=stock/sector/market`），并在前端页面确保这些新闻组件 **严格跟随当前选中的标的代码 (Symbol)、其所属板块，以及大盘环境** 进行请求和渲染，禁止显示毫不相关的噪音新闻。
> 6. **自测与回执**: 完成 C# 业务和 Migration，且保障 `dotnet test` 后，清空此回执，编写 `Step 2 开发完成回执`。 
>
> *(收到此指令后，请立即开始编写 直到完成任务)*
