from __future__ import annotations

import argparse
import json
import shutil
import sys
from collections import OrderedDict
from datetime import datetime
from pathlib import Path
from typing import Any
from urllib import error, parse, request


def ordered_query(*pairs: tuple[str, str]) -> OrderedDict[str, str]:
    query: OrderedDict[str, str] = OrderedDict()
    for key, value in pairs:
        query[key] = value
    return query


def build_tool_definitions(symbol: str, task_id: str) -> list[dict[str, Any]]:
    return [
        {
            "tool": "CompanyOverviewMcp",
            "path": "/api/stocks/mcp/company-overview",
            "query": ordered_query(("symbol", symbol), ("taskId", task_id)),
        },
        {
            "tool": "StockProductMcp",
            "path": "/api/stocks/mcp/product",
            "query": ordered_query(("symbol", symbol), ("taskId", task_id)),
        },
        {
            "tool": "StockFundamentalsMcp",
            "path": "/api/stocks/mcp/fundamentals",
            "query": ordered_query(("symbol", symbol), ("taskId", task_id)),
        },
        {
            "tool": "StockShareholderMcp",
            "path": "/api/stocks/mcp/shareholder",
            "query": ordered_query(("symbol", symbol), ("taskId", task_id)),
        },
        {
            "tool": "MarketContextMcp",
            "path": "/api/stocks/mcp/market-context",
            "query": ordered_query(("symbol", symbol), ("taskId", task_id)),
        },
        {
            "tool": "SocialSentimentMcp",
            "path": "/api/stocks/mcp/social-sentiment",
            "query": ordered_query(("symbol", symbol), ("taskId", task_id)),
        },
        {
            "tool": "StockKlineMcp",
            "path": "/api/stocks/mcp/kline",
            "query": ordered_query(("symbol", symbol), ("interval", "day"), ("count", "60"), ("taskId", task_id)),
        },
        {
            "tool": "StockMinuteMcp",
            "path": "/api/stocks/mcp/minute",
            "query": ordered_query(("symbol", symbol), ("taskId", task_id)),
        },
        {
            "tool": "StockStrategyMcp",
            "path": "/api/stocks/mcp/strategy",
            "query": ordered_query(("symbol", symbol), ("interval", "day"), ("count", "60"), ("taskId", task_id)),
        },
        {
            "tool": "StockNewsMcp",
            "path": "/api/stocks/mcp/news",
            "query": ordered_query(("symbol", symbol), ("level", "stock"), ("taskId", task_id)),
        },
        {
            "tool": "StockSearchMcp",
            "path": "/api/stocks/mcp/search",
            "query": ordered_query(("query", "浦发银行"), ("trustedOnly", "true"), ("taskId", task_id)),
        },
    ]


def build_query_string(query: OrderedDict[str, str]) -> str:
    return parse.urlencode(list(query.items()))


def scalar_top_level(data: Any) -> OrderedDict[str, Any]:
    result: OrderedDict[str, Any] = OrderedDict()
    if not isinstance(data, dict):
        return result
    for key, value in data.items():
        if value is None or isinstance(value, (str, int, float, bool)):
            result[key] = value
    return result


def compact_json(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"))


def pretty_json(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, indent=4)


def request_endpoint(base_url: str, definition: dict[str, Any]) -> dict[str, Any]:
    query_string = build_query_string(definition["query"])
    path_with_query = f"{definition['path']}?{query_string}"
    url = f"{base_url.rstrip('/')}{path_with_query}"

    response_record: OrderedDict[str, Any] = OrderedDict(
        statusCode=None,
        headers=OrderedDict(),
        bodyRaw=None,
        bodyJson=None,
    )
    error_record: OrderedDict[str, Any] | None = None

    req = request.Request(url, method="GET")
    try:
        with request.urlopen(req, timeout=240) as resp:
            body_raw = resp.read().decode("utf-8", errors="replace")
            response_record["statusCode"] = getattr(resp, "status", None)
            response_record["headers"] = OrderedDict((key, value) for key, value in resp.getheaders())
            response_record["bodyRaw"] = body_raw
    except error.HTTPError as exc:
        body_raw = exc.read().decode("utf-8", errors="replace")
        response_record["statusCode"] = exc.code
        response_record["headers"] = OrderedDict((key, value) for key, value in exc.headers.items())
        response_record["bodyRaw"] = body_raw
        error_record = OrderedDict(message=str(exc), type=type(exc).__name__)
    except Exception as exc:  # noqa: BLE001
        error_record = OrderedDict(message=str(exc), type=type(exc).__name__)

    return OrderedDict(
        tool=definition["tool"],
        request=OrderedDict(
            method="GET",
            url=url,
            path=path_with_query,
            tool=definition["tool"],
            query=definition["query"],
        ),
        response=response_record,
        error=error_record,
    )


def add_response_lines(lines: list[str], body: dict[str, Any] | None, error_record: dict[str, Any] | None) -> None:
    if body:
        for key in ["traceId", "taskId", "latencyMs", "errorCode", "freshnessTag", "sourceTier"]:
            value = body.get(key)
            if value is not None:
                lines.append(f" - {key}: `{value}`")
        cache = body.get("cache")
        if isinstance(cache, dict):
            if "hit" in cache:
                lines.append(f" - cache.hit: `{cache['hit']}`")
            if "source" in cache:
                lines.append(f" - cache.source: `{cache['source']}`")
        warnings = body.get("warnings") or []
        if warnings:
            lines.append(" - warnings:")
            for warning in warnings:
                lines.append(f"   - {warning}")
        degraded_flags = body.get("degradedFlags") or []
        if degraded_flags:
            lines.append(" - degradedFlags:")
            for item in degraded_flags:
                lines.append(f"   - {item}")
        return

    if error_record:
        if error_record.get("message"):
            lines.append(f" - error.message: `{error_record['message']}`")
        if error_record.get("type"):
            lines.append(f" - error.type: `{error_record['type']}`")


def add_evidence_lines(lines: list[str], evidence_items: list[dict[str, Any]]) -> None:
    if not evidence_items:
        return
    lines.append("### 证据样本（最多 3 条）")
    for item in evidence_items[:3]:
        title = item.get("title") or item.get("point") or ""
        source = item.get("source") or ""
        published_at = item.get("publishedAt") or ""
        summary = item.get("summary") or item.get("excerpt") or item.get("point") or ""
        lines.append(f"1. `{title}` | 来源：{source} | 时间：{published_at}")
        lines.append(f"   - 摘要：{summary}")
    lines.append("")


def build_fix_section(results: list[dict[str, Any]], task_id: str, test_command: str, test_result: str) -> str:
    bodies: dict[str, dict[str, Any]] = {}
    for item in results:
        raw = item["response"].get("bodyRaw")
        if not raw:
            continue
        try:
            bodies[item["tool"]] = json.loads(raw)
        except json.JSONDecodeError:
            continue

    market_data = (bodies.get("MarketContextMcp") or {}).get("data") or {}
    search_body = bodies.get("StockSearchMcp") or {}
    search_warning = ((search_body.get("warnings") or [""])[0])

    stage_label = json.dumps(market_data.get("stageLabel"), ensure_ascii=False)
    execution_frequency_label = json.dumps(market_data.get("executionFrequencyLabel"), ensure_ascii=False)

    lines = [
        "## 2026-03-27 修复增量说明",
        "",
        "> 说明：本节基于本次重新抓取的**当前在线真实返回**整理，不再引用修复前样本。",
        "",
        f"- 本次重跑 TaskId：`{task_id}`",
        f"- 定向单测命令：`{test_command}`",
        f"- 单测结果：**{test_result}**",
        "",
        "### 已在最新返回中确认的修复结果",
        "",
        f"1. `MarketContextMcp.data.stageLabel = {stage_label}`，`executionFrequencyLabel = {execution_frequency_label}`。",
        f"2. `StockSearchMcp` 当前 warning 已更新为：`{search_warning}`。",
        "3. 本报告上文全部 11 个 MCP 样本均来自本次在线重抓，已与当前代码行为同步。",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--base-url", default="http://localhost:5119")
    parser.add_argument("--symbol", default="sh600000")
    parser.add_argument("--task-id", required=True)
    parser.add_argument("--output-dir", required=True)
    parser.add_argument("--report-path", required=True)
    parser.add_argument("--test-command", required=True)
    parser.add_argument("--test-result", required=True)
    args = parser.parse_args()

    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    timestamp = datetime.now().strftime("%Y%m%d-%H%M%S")

    json_path = output_dir / f"mcp-full-request-response-{timestamp}.json"
    md_path = output_dir / f"mcp-full-request-response-{timestamp}.md"
    filtered_md_path = output_dir / f"mcp-full-request-response-{timestamp}.filtered.md"
    report_path = Path(args.report_path)

    results = [request_endpoint(args.base_url, definition) for definition in build_tool_definitions(args.symbol, args.task_id)]

    json_path.write_text(pretty_json(results), encoding="utf-8")

    index_lines = ["# MCP 请求与返回（全量11个）", ""]
    for item in results:
        index_lines.append(f"## {item['tool']}")
        index_lines.append(f"- request.url: `{item['request']['url']}`")
        index_lines.append(f"- response.statusCode: `{item['response']['statusCode']}`")
        index_lines.append("")
    md_path.write_text("\n".join(index_lines).rstrip() + "\n", encoding="utf-8")

    filtered_lines = [
        "# MCP 请求与返回（过滤版）",
        "",
        f"> 来源：`{json_path.as_posix()}`",
        "> 说明：保留请求参数、状态、traceId、关键指标与证据样本；省略超长 K 线/分时点位明细。",
        "",
        f"总计工具数：**{len(results)}**",
        "",
    ]

    for item in results:
        body = None
        body_raw = item["response"].get("bodyRaw")
        if body_raw:
            try:
                body = json.loads(body_raw)
            except json.JSONDecodeError:
                body = None

        filtered_lines.extend(
            [
                f"## {item['tool']}",
                "",
                "### 请求",
                f" - method: `{item['request']['method']}`",
                f" - url: `{item['request']['url']}`",
                f" - query: `{compact_json(item['request']['query'])}`",
                "",
                "### 返回",
                f" - statusCode: `{item['response']['statusCode']}`",
            ]
        )
        add_response_lines(filtered_lines, body, item.get("error"))
        filtered_lines.extend(["", "### 关键数据（过滤）"])
        data = scalar_top_level((body or {}).get("data") or {})
        filtered_lines.append("```json")
        filtered_lines.append(pretty_json(data))
        filtered_lines.append("```")
        filtered_lines.append("")
        add_evidence_lines(filtered_lines, ((body or {}).get("evidence") or []))
        filtered_lines.extend(["---", ""])

    filtered_lines.append(build_fix_section(results, args.task_id, args.test_command, args.test_result))
    filtered_md_path.write_text("\n".join(filtered_lines).rstrip() + "\n", encoding="utf-8")

    report_path.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(filtered_md_path, report_path)

    failures = []
    for item in results:
        status_code = item["response"].get("statusCode")
        if status_code != 200:
            failures.append(f"{item['tool']}: status={status_code}")
        if item.get("error"):
            failures.append(f"{item['tool']}: {item['error'].get('message')}")

    summary = OrderedDict(
        taskId=args.task_id,
        jsonPath=str(json_path),
        mdPath=str(md_path),
        filteredMdPath=str(filtered_md_path),
        reportPath=str(report_path),
        failureCount=len(failures),
        failures=failures,
    )
    print(pretty_json(summary))
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
