#!/usr/bin/env python3
"""Run one market sync + audit check and persist a JSON report.

Behavior:
- POST /api/market/sync
- GET  /api/market/audit
- Read required source keys from audit.sources
- Read latest recentSyncs reasons (if any)

Exit codes:
- 1: all sources healthy but businessComplete=false (partial recovery)
- 2: sync request failed
- 3: audit request failed or required key missing
- 4: any required source has status=error
- 0: all sources healthy and businessComplete=true
"""

from __future__ import annotations

import argparse
import json
import sys
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List
from urllib.error import HTTPError, URLError
from urllib.parse import urljoin
from urllib.request import Request, urlopen

DEFAULT_BASE_URL = "http://localhost:5119"
DEFAULT_TIMEOUT_SECONDS = 30

SYNC_PATH = "/api/market/sync"
AUDIT_PATH = "/api/market/audit"

REQUIRED_SOURCE_KEYS: List[str] = [
    "bkzj_board_rankings",
    "bkzj_board_rankings_industry",
    "bkzj_board_rankings_concept",
    "bkzj_board_rankings_style",
    "ths_continuous_limit_up",
    "eastmoney_market_fs_sh_sz",
]


class RequestFailure(RuntimeError):
    pass


def now_local_text() -> str:
    return datetime.now().strftime("%Y-%m-%d %H:%M:%S")


def now_stamp() -> str:
    return datetime.now().strftime("%Y%m%d-%H%M%S")


def request_json(base_url: str, method: str, path: str, timeout_seconds: int) -> Dict[str, Any]:
    url = urljoin(base_url.rstrip("/") + "/", path.lstrip("/"))
    req = Request(url=url, method=method.upper())
    data = None

    if method.upper() == "POST":
        req.add_header("Content-Type", "application/json")
        data = b"{}"

    try:
        with urlopen(req, data=data, timeout=timeout_seconds) as resp:
            body = resp.read().decode("utf-8", errors="replace")
    except HTTPError as exc:
        raise RequestFailure(f"HTTP {exc.code} {exc.reason} @ {path}") from exc
    except URLError as exc:
        raise RequestFailure(f"URL error @ {path}: {exc.reason}") from exc

    if not body.strip():
        return {}

    try:
        return json.loads(body)
    except json.JSONDecodeError as exc:
        raise RequestFailure(f"Invalid JSON @ {path}: {exc}") from exc


def extract_source_status_map(audit_payload: Dict[str, Any]) -> Dict[str, str]:
    sources = audit_payload.get("sources", [])
    if not isinstance(sources, list):
        return {}

    status_map: Dict[str, str] = {}
    for item in sources:
        if not isinstance(item, dict):
            continue

        name = item.get("name")
        status = item.get("status")
        if isinstance(name, str):
            status_map[name] = "" if status is None else str(status)

    return status_map


def extract_latest_reasons(audit_payload: Dict[str, Any]) -> Any:
    recent_syncs = audit_payload.get("recentSyncs", [])
    if not isinstance(recent_syncs, list) or not recent_syncs:
        return None

    latest = recent_syncs[0]
    if not isinstance(latest, dict):
        return None

    return latest.get("reasons")


def extract_latest_sync_completion(audit_payload: Dict[str, Any]) -> tuple:
    """Return (source_healthy, business_complete) from the latest recentSync entry.
    Returns (None, None) if data is unavailable."""
    recent_syncs = audit_payload.get("recentSyncs", [])
    if not isinstance(recent_syncs, list) or not recent_syncs:
        return None, None
    latest = recent_syncs[0]
    if not isinstance(latest, dict):
        return None, None
    source_healthy = latest.get("sourceHealthy")
    business_complete = latest.get("businessComplete")
    return source_healthy, business_complete


def format_reasons_for_console(reasons: Any) -> str:
    if reasons is None:
        return "(none)"
    if isinstance(reasons, list):
        if not reasons:
            return "[]"
        return "; ".join(str(item) for item in reasons)
    return str(reasons)


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Run one /api/market/sync + /api/market/audit check and write a JSON log."
    )
    parser.add_argument(
        "--base-url",
        default=DEFAULT_BASE_URL,
        help=f"API base URL (default: {DEFAULT_BASE_URL})",
    )
    parser.add_argument(
        "--timeout",
        type=int,
        default=DEFAULT_TIMEOUT_SECONDS,
        help=f"Request timeout seconds (default: {DEFAULT_TIMEOUT_SECONDS})",
    )
    return parser


def main() -> int:
    args = build_parser().parse_args()
    base_url = str(args.base_url)
    timeout_seconds = int(args.timeout)

    repo_root = Path(__file__).resolve().parents[1]
    logs_dir = repo_root / "logs"
    logs_dir.mkdir(parents=True, exist_ok=True)
    log_path = logs_dir / f"market-audit-check-{now_stamp()}.json"

    started_at = now_local_text()

    result: Dict[str, Any] = {
        "startedAtLocal": started_at,
        "endedAtLocal": None,
        "baseUrl": base_url,
        "sync": {"ok": False, "error": None, "response": None},
        "audit": {"ok": False, "error": None, "response": None},
        "requiredSourceKeys": REQUIRED_SOURCE_KEYS,
        "sourceStatuses": {},
        "missingKeys": [],
        "latestRecentSyncReasons": None,
        "latestSyncSourceHealthy": None,
        "latestSyncBusinessComplete": None,
        "exitCode": None,
    }

    exit_code = 0

    try:
        sync_payload = request_json(base_url, "POST", SYNC_PATH, timeout_seconds)
        result["sync"]["ok"] = True
        result["sync"]["response"] = sync_payload
    except Exception as exc:  # noqa: BLE001
        result["sync"]["ok"] = False
        result["sync"]["error"] = str(exc)
        exit_code = 2

    if exit_code == 0:
        try:
            audit_payload = request_json(base_url, "GET", AUDIT_PATH, timeout_seconds)
            result["audit"]["ok"] = True
            result["audit"]["response"] = audit_payload

            source_status_map = extract_source_status_map(audit_payload)
            source_statuses: Dict[str, str] = {}
            missing_keys: List[str] = []

            for key in REQUIRED_SOURCE_KEYS:
                if key not in source_status_map:
                    missing_keys.append(key)
                else:
                    source_statuses[key] = source_status_map[key]

            result["sourceStatuses"] = source_statuses
            result["missingKeys"] = missing_keys
            result["latestRecentSyncReasons"] = extract_latest_reasons(audit_payload)
            source_healthy, business_complete = extract_latest_sync_completion(audit_payload)
            result["latestSyncSourceHealthy"] = source_healthy
            result["latestSyncBusinessComplete"] = business_complete

            if missing_keys:
                exit_code = 3
            else:
                has_error_status = any(
                    str(source_statuses.get(key, "")).strip().lower() == "error"
                    for key in REQUIRED_SOURCE_KEYS
                )
                if has_error_status:
                    exit_code = 4
                elif source_healthy is True and business_complete is False:
                    exit_code = 1  # partial recovery: fetches ok but business degraded
        except Exception as exc:  # noqa: BLE001
            result["audit"]["ok"] = False
            result["audit"]["error"] = str(exc)
            exit_code = 3

    result["endedAtLocal"] = now_local_text()
    result["exitCode"] = exit_code
    log_path.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")

    print("=== Market Audit Daily Check (Once) ===")
    print(f"Base URL: {base_url}")
    print(f"Sync: {'OK' if result['sync']['ok'] else 'FAIL'}")
    if result["sync"]["error"]:
        print(f"  sync_error: {result['sync']['error']}")

    if result["audit"]["ok"]:
        print("Audit: OK")
        print("Source statuses:")
        for key in REQUIRED_SOURCE_KEYS:
            status = result["sourceStatuses"].get(key, "<MISSING>")
            print(f"  - {key}: {status}")
        sh = result.get("latestSyncSourceHealthy")
        bc = result.get("latestSyncBusinessComplete")
        sh_text = str(sh) if sh is not None else "n/a"
        bc_text = str(bc) if bc is not None else "n/a"
        print(f"sourceHealthy: {sh_text}  businessComplete: {bc_text}")
        print(f"Latest reasons: {format_reasons_for_console(result['latestRecentSyncReasons'])}")
        if result["missingKeys"]:
            print(f"Missing keys: {', '.join(result['missingKeys'])}")
    else:
        print("Audit: FAIL")
        if result["audit"]["error"]:
            print(f"  audit_error: {result['audit']['error']}")

    print(f"Log written: {log_path}")
    print(f"Exit code: {exit_code}")

    return exit_code


if __name__ == "__main__":
    sys.exit(main())
