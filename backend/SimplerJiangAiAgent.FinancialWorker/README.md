# SimplerJiangAiAgent.FinancialWorker

## 简介

SimplerJiangAiAgent.FinancialWorker 是一个基于 .NET 8 的 minimal API + background worker 服务，面向 A 股财务数据采集与本地落库场景。当前服务监听 `http://localhost:5120`，提供手动采集接口、健康检查接口和定时调度能力，并将结果存储到数据根目录下的 LiteDB 与 PDF 文件目录中。

这个项目已经具备独立服务的基本形态，但目前仍与父仓库存在耦合，若要作为独立开源项目发布，仍需要做进一步抽离与整理。

## 功能特性

- 基于 .NET 8 minimal API 暴露采集、查询、配置和日志接口
- 内置 background worker，按中国标准时间窗口执行定时采集
- 健康检查接口：`/health`
- 单股票采集、批量采集、PDF 补采集三类触发入口
- 使用 LiteDB 持久化财务报表、财务指标、分红、融资融券、采集日志和配置
- 支持将下载的 PDF 报告缓存到本地目录，便于后续解析或补采
- 数据根目录可通过环境变量 `SJAI_DATA_ROOT` 覆盖

## 架构与组件

- `Program.cs`
  - 启动 minimal API
  - 监听 `http://localhost:5120`
  - 注册 HTTP 客户端、LiteDB 上下文、采集编排器、PDF 处理组件和后台 Worker
  - 暴露全部 HTTP 端点
- `Worker.cs`
  - 负责定时调度
  - 使用 `China Standard Time`
  - 仅在 `15:30-23:00` 活跃窗口内运行
  - 采用 30 秒轮询周期
  - 基于 `Enabled`、`Scope`、`Frequency`、`WatchlistSymbols` 配置决定是否执行
  - 对 daily/weekly 调度做重复运行抑制
- `Data/FinancialDbContext.cs`
  - 管理 LiteDB 连接与集合
  - 负责集合初始化与索引建立
- 数据采集与 PDF 处理
  - 数据源 HTTP 访问通过 `Microsoft.Extensions.Http`
  - PDF 补充能力使用 `PdfPig`、`Docnet.Core`、`itext7`
  - 本地持久化使用 `LiteDB`

## 数据源策略

当前数据源策略按以下降级顺序执行：

`emweb -> datacenter -> ths -> optional PDF supplement`

- 首选：Eastmoney emweb
- 次选：Eastmoney datacenter
- 再次选：THS
- 可选补充：CNINFO PDF supplement

其中 PDF supplement 为可选补充路径，用于在前序结构化数据不足时，通过 PDF 下载与解析链路补充信息。当前 PDF 补充实现依赖 `PdfPig`、`Docnet.Core`、`itext7`。

## API

以下端点仅列出 `Program.cs` 中当前实际暴露的接口：

| Method | Path | 说明 |
| --- | --- | --- |
| GET | `/health` | 健康检查 |
| GET | `/api/config` | 读取当前采集配置 |
| PUT | `/api/config` | 更新当前采集配置 |
| POST | `/api/collect/{symbol}` | 触发单股票采集 |
| POST | `/api/collect-batch` | 触发批量采集 |
| POST | `/api/pdf-collect/{symbol}` | 触发单股票 PDF 补采集 |
| GET | `/api/reports/{symbol}` | 查询财务报表 |
| GET | `/api/indicators/{symbol}` | 查询财务指标 |
| GET | `/api/dividends/{symbol}` | 查询分红数据 |
| GET | `/api/margin/{symbol}` | 查询融资融券数据 |
| GET | `/api/logs` | 查询采集日志 |

示例：健康检查

```bash
curl http://localhost:5120/health
```

示例：触发单股票采集

```bash
curl -X POST http://localhost:5120/api/collect/600519
```

## 快速开始

### 运行

在仓库根目录执行：

```bash
dotnet run --project backend/SimplerJiangAiAgent.FinancialWorker/SimplerJiangAiAgent.FinancialWorker.csproj
```

服务启动后默认监听：`http://localhost:5120`

### 基本验证

```bash
curl http://localhost:5120/health
curl -X POST http://localhost:5120/api/collect/600519
```

## 配置与存储

### 数据根目录

- 可通过环境变量 `SJAI_DATA_ROOT` 覆盖数据根目录

### 本地存储

- LiteDB 文件：`App_Data/financial-data.db`
- 下载 PDF 目录：`App_Data/financial-reports`

### 持久化集合

`FinancialDbContext` 当前维护以下集合：

- `financial_reports`
- `financial_indicators`
- `dividends`
- `margin_trading`
- `collection_logs`
- `config`

### 调度配置

后台 Worker 当前基于以下配置项决定行为：

- `Enabled`
- `Scope`
- `Frequency`
- `WatchlistSymbols`

调度规则来自 `Worker.cs` 的当前实现：

- 时区：`China Standard Time`
- 活跃窗口：`15:30-23:00`
- 轮询周期：30 秒
- 重复执行抑制：daily/weekly

## 项目结构

```text
backend/SimplerJiangAiAgent.FinancialWorker/
|- Program.cs
|- Worker.cs
|- Data/
|  \- FinancialDbContext.cs
|- Models/
|- Services/
|  |- EastmoneyFinanceClient.cs
|  |- EastmoneyDatacenterClient.cs
|  |- ThsFinanceClient.cs
|  |- CninfoClient.cs
|  \- Pdf/
\- SimplerJiangAiAgent.FinancialWorker.csproj
```

## 当前耦合与抽离说明

这个项目适合作为未来独立仓库的候选，但当前仍不是可直接单独发布的开源包。主要原因不是功能缺失，而是它仍然处于父仓库上下文中运行和维护。

在独立发布前，至少还需要继续完成以下抽离工作：

- 梳理并显式化与父仓库共享的运行时路径和配置约定
- 补齐独立项目级文档、示例数据和开发者入口
- 进一步确认最小依赖边界，减少对父仓库目录结构的默认假设
- 在独立仓库语境下重新整理启动、配置和发布体验

## Roadmap Ideas

- 提炼更清晰的独立配置模型，降低对父仓库运行环境的隐式依赖
- 将数据源适配层与 HTTP API/Worker 启动层进一步解耦
- 为独立发布补齐更完整的本地开发、测试和样例数据说明
- 继续明确 PDF supplement 的启用边界与失败回退行为
- 在保持当前降级顺序的前提下，进一步提升抽离后的可维护性与可复用性