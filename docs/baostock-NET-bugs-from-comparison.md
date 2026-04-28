# baostock.NET Bug 报告（StockCopilot 集成对比测试发现）

测试日期：2026-04-27  
测试版本：baostock.NET v1.4.0-dev (commit d44abb0)  
测试环境：Windows, .NET 9, TestUI http://localhost:5050  
测试人：PM Agent（StockCopilot v0.4.7 集成前评估）

---

## Bug #1: 单股实时行情接口始终返回 SH600519 数据（Critical）

**端点**: `POST /api/multi/realtime-quote`

**复现步骤**:
1. 启动 TestUI，登录 baostock
2. 发送请求：
   ```
   POST /api/multi/realtime-quote
   Content-Type: application/json
   {"code":"SZ000001"}
   ```
3. 返回数据中 `code = "SH600519"`，应为 `SZ000001`

**实测**:
| 请求 code | 返回 code | 预期 | 结果 |
|-----------|-----------|------|------|
| SZ000001 | SH600519 | SZ000001 平安银行 | ❌ 返回茅台 |
| SH000001 | SH600519 | SH000001 上证指数 | ❌ 返回茅台 |
| SH600519 | SH600519 | SH600519 茅台 | ✅ 正确（碰巧） |

**注意**: 批量接口 `POST /api/multi/realtime-quotes` 传入 `{"codes":["SH600519","SZ000001","SH000001"]}` 可以正确返回 3 只不同股票的数据。

**可能原因**: 单股接口内部可能复用了上一次请求的 code 缓存，或者没有正确将请求参数传递给底层 hedged request 逻辑。

**影响**: 单股实时行情接口完全不可用，必须用批量接口替代。

---

## Bug #2: 巨潮公告翻页不生效（Critical）

**端点**: `POST /api/cninfo/announcements`

**复现步骤**:
1. 发送 page=1 请求：
   ```
   POST /api/cninfo/announcements
   Content-Type: application/json
   {"code":"SH600519","startDate":"2024-01-01","page":1,"pageSize":10}
   ```
   返回 10 条记录。
2. 发送 page=2 请求：
   ```
   POST /api/cninfo/announcements
   Content-Type: application/json
   {"code":"SH600519","startDate":"2024-01-01","page":2,"pageSize":10}
   ```
   返回的 10 条记录与 page=1 **完全相同**（标题、日期、ID 一模一样）。

**预期**: page=2 应返回第 11-20 条公告。

**可能原因**: 翻页参数 `page` 没有正确传递给巨潮 API 的底层 HTTP 请求，或者巨潮 API 的分页参数名与代码中使用的不一致。

**影响**: 只能获取第一页公告（最新 10 条），无法获取历史公告全量数据。

---

## Bug #3: 巨潮公告类别筛选无效（Medium）

**端点**: `POST /api/cninfo/announcements`

**复现步骤**:
1. 发送带类别筛选的请求：
   ```
   POST /api/cninfo/announcements
   Content-Type: application/json
   {"code":"SH600519","category":"AnnualReport","startDate":"2024-01-01"}
   ```
2. 返回 `rowCount: 0`，无任何数据。

**预期**: 应返回 600519 的 2024 年以来年报公告（至少 1 条 2024 年报）。

**补充测试**:
- 不传 category 参数时能正常返回 10 条（混合类型公告）
- 说明 category 参数的值可能不匹配巨潮 API 的实际分类编码

**可能原因**: `CninfoAnnouncementCategory.AnnualReport` 映射到的巨潮 API 参数值不正确。巨潮 API 使用 `category` 为 `category_ndbg_szsh`（年报）等编码，需确认映射关系。

**影响**: 无法按公告类型筛选，只能获取全部类型混合列表。

---

## Bug #4: PowerShell 5.1 内联 JSON 传参异常（Minor）

**端点**: `POST /api/multi/realtime-quotes` 等需要 JSON body 的端点

**复现步骤**:
在 Windows PowerShell 5.1 中用 `Invoke-RestMethod -Body '{"codes":["SH600519"]}'` 发送请求，服务端返回 400 或解析错误。

**解决方法**: 必须将 JSON 写入临时文件，用 `@file` 方式传参，或使用 PowerShell 7+。

**影响**: 仅影响 PowerShell 5.1 用户的手动测试体验，不影响 .NET SDK 调用。TestUI 文档可加说明。

---

## 非 Bug 但建议改进

### 建议 1: 实时行情接口增加衍生字段

当前 `GetRealtimeQuoteAsync` 返回原始行情字段（last/open/high/low/preClose/volume/amount/bid/ask），但缺少常用的衍生计算字段：
- `change` (涨跌额 = last - preClose)
- `changePercent` (涨跌幅)
- `turnoverRate` (换手率)

StockCopilot 用户高度依赖这些字段。建议在 `RealtimeQuote` record 中增加计算属性，或提供 `RealtimeQuoteEx` 扩展。

### 建议 2: 财报三表增加结构化汇总字段

当前 `QueryFullBalanceSheetAsync` 返回 `rawFields` 字典但缺少结构化的顶层汇总字段（如 `TotalAssets`、`TotalLiabilities`、`ShareholderEquity`）。建议增加常用字段的强类型属性，降低消费端解析成本。

### 建议 3: 公告接口增加总条数字段

当前 `/api/cninfo/announcements` 返回 `rowCount`（本页条数），但不返回 `totalCount`（总条数），消费端无法计算总页数。

---

## 环境信息

- OS: Windows 11
- .NET: 9.0
- baostock.NET commit: d44abb0
- TestUI 端口: 5050
- 对照系统: StockCopilot v0.4.7 (http://localhost:5119)
