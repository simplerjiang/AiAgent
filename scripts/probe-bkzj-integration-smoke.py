#!/usr/bin/env python3
"""Run integration smoke probes for selected market data sources.

Targets (20 rounds by default):
- bkzj_industry (mapped to source key bkzj_board_rankings_industry)
- bkzj_concept (mapped to source key bkzj_board_rankings_concept)
- bkzj_style (mapped to source key bkzj_board_rankings_style)
- ths_continuous_limit_up
- eastmoney_market_fs_sh_sz (audit recorded, final pass uses direct probe B)

The script triggers `/api/market/sync` and inspects `/api/market/audit` for source status,
then prints per-source stats and writes a timestamped JSON report under `logs/`.
Exit code is 0 only when all targets pass.
"""

from __future__ import annotations

import json
import os
import signal
import subprocess
import sys
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, List, Optional, Tuple
from urllib.error import HTTPError, URLError
from urllib.parse import urljoin
from urllib.request import Request, urlopen


ROUND_COUNT = 20
REQUEST_TIMEOUT_SECONDS = 45
BOOT_WAIT_SECONDS = 120
HEALTH_POLL_SECONDS = 2

BASE_URL = os.environ.get("BKZJ_PROBE_BASE_URL", "http://localhost:5119")
HEALTH_PATH = "/api/health"
SYNC_PATH = "/api/market/sync"
AUDIT_PATH = "/api/market/audit"

TARGETS: Dict[str, str] = {
    "bkzj_industry": "bkzj_board_rankings_industry",
    "bkzj_concept": "bkzj_board_rankings_concept",
    "bkzj_style": "bkzj_board_rankings_style",
    "ths_continuous_limit_up": "ths_continuous_limit_up",
    "eastmoney_market_fs_sh_sz": "eastmoney_market_fs_sh_sz",
}

DIRECT_PROBE_TARGET = "eastmoney_market_fs_sh_sz"
DIRECT_PROBE_URL = (
    "https://push2.eastmoney.com/api/qt/ulist.np/get?"
    "fltt=2&fields=f12,f13,f14,f2,f3,f4,f5,f6,f8,f9,f10,f15,f16,f124,f152&"
    "secids=1.000001,0.399001"
)


@dataclass
class RoundResult:
    round_no: int
    timestamp_utc: str
    sync_ok: bool
    source_status: Dict[str, Optional[str]]
    direct_probe: Dict[str, object]
    error: Optional[str]


class HttpError(RuntimeError):
    pass


def utc_now_iso() -> str:
    return datetime.now(timezone.utc).isoformat()


def request_json(method: str, path: str) -> dict:
    url = urljoin(BASE_URL.rstrip("/") + "/", path.lstrip("/"))
    req = Request(url=url, method=method.upper())
    if method.upper() == "POST":
        req.add_header("Content-Type", "application/json")
        payload = b"{}"
    else:
        payload = None

    try:
        with urlopen(req, payload, timeout=REQUEST_TIMEOUT_SECONDS) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            if not body.strip():
                return {}
            return json.loads(body)
    except HTTPError as exc:
        raise HttpError(f"HTTP {exc.code} {exc.reason} @ {path}") from exc
    except URLError as exc:
        raise HttpError(f"URL error @ {path}: {exc.reason}") from exc
    except json.JSONDecodeError as exc:
        raise HttpError(f"Invalid JSON @ {path}: {exc}") from exc


def request_external_json(url: str) -> dict:
    req = Request(url=url, method="GET")
    req.add_header("User-Agent", "Mozilla/5.0")
    req.add_header("Referer", "https://quote.eastmoney.com/")

    try:
        with urlopen(req, timeout=REQUEST_TIMEOUT_SECONDS) as resp:
            body = resp.read().decode("utf-8", errors="replace")
            if not body.strip():
                return {}
            return json.loads(body)
    except HTTPError as exc:
        raise HttpError(f"HTTP {exc.code} {exc.reason} @ direct probe") from exc
    except URLError as exc:
        raise HttpError(f"URL error @ direct probe: {exc.reason}") from exc
    except json.JSONDecodeError as exc:
        raise HttpError(f"Invalid JSON @ direct probe: {exc}") from exc


def probe_direct_turnover_b() -> Dict[str, object]:
    payload = request_external_json(DIRECT_PROBE_URL)
    data = payload.get("data", {}) if isinstance(payload, dict) else {}
    diff = data.get("diff", []) if isinstance(data, dict) else []
    if not isinstance(diff, list):
        raise HttpError("direct probe diff is not an array")

    total_turnover = 0.0
    valid_rows = 0
    for item in diff:
        if not isinstance(item, dict):
            continue
        try:
            total_turnover += max(0.0, float(item.get("f6") or 0.0))
            valid_rows += 1
        except (TypeError, ValueError):
            continue

    return {
        "ok": valid_rows >= 2 and total_turnover > 0,
        "rowCount": len(diff),
        "validRows": valid_rows,
        "totalTurnover": round(total_turnover, 2),
    }


def api_is_ready() -> bool:
    try:
        request_json("GET", HEALTH_PATH)
        return True
    except Exception:
        return False


def start_backend_if_needed(repo_root: Path) -> Tuple[Optional[subprocess.Popen], bool]:
    if api_is_ready():
        return None, False

    cmd = [
        "dotnet",
        "run",
        "--project",
        r".\backend\SimplerJiangAiAgent.Api\SimplerJiangAiAgent.Api.csproj",
        "--launch-profile",
        "http",
    ]
    process = subprocess.Popen(
        cmd,
        cwd=str(repo_root),
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
        stdin=subprocess.DEVNULL,
        text=True,
        encoding="utf-8",
        errors="replace",
        bufsize=1,
    )

    deadline = time.time() + BOOT_WAIT_SECONDS

    while time.time() < deadline:
        if process.poll() is not None:
            raise RuntimeError(
                "Backend process exited before health became ready. "
                f"ExitCode={process.returncode}."
            )

        if api_is_ready():
            return process, True

        time.sleep(HEALTH_POLL_SECONDS)

    raise TimeoutError(f"Backend did not become ready within {BOOT_WAIT_SECONDS}s")


def terminate_process(process: subprocess.Popen) -> None:
    if process.poll() is not None:
        return

    try:
        process.terminate()
        process.wait(timeout=10)
        return
    except Exception:
        pass

    try:
        process.kill()
        process.wait(timeout=5)
    except Exception:
        pass


def run_probe(round_count: int) -> Tuple[List[RoundResult], Dict[str, dict]]:
    rounds: List[RoundResult] = []
    counters = {
        target: {"ok": 0, "fail": 0, "statuses": []}
        for target in TARGETS
    }
    direct_probe_counter = {"ok": 0, "fail": 0, "results": []}

    for idx in range(1, round_count + 1):
        error: Optional[str] = None
        sync_ok = False
        source_status: Dict[str, Optional[str]] = {target: None for target in TARGETS}
        direct_probe: Dict[str, object] = {"ok": False, "error": None}

        try:
            _ = request_json("POST", SYNC_PATH)
            sync_ok = True
            audit = request_json("GET", AUDIT_PATH)
            source_items = audit.get("sources", []) if isinstance(audit, dict) else []
            by_name = {
                str(item.get("name", "")): str(item.get("status", ""))
                for item in source_items
                if isinstance(item, dict)
            }

            for target_name, source_key in TARGETS.items():
                status = by_name.get(source_key)
                source_status[target_name] = status
                if status == "ok":
                    counters[target_name]["ok"] += 1
                else:
                    counters[target_name]["fail"] += 1
                counters[target_name]["statuses"].append(status)

            direct_probe = probe_direct_turnover_b()
            if direct_probe.get("ok") is True:
                direct_probe_counter["ok"] += 1
            else:
                direct_probe_counter["fail"] += 1
            direct_probe_counter["results"].append(direct_probe)
        except Exception as exc:  # noqa: BLE001
            error = str(exc)
            for target_name in TARGETS:
                counters[target_name]["fail"] += 1
                counters[target_name]["statuses"].append(None)
            direct_probe = {"ok": False, "error": error}
            direct_probe_counter["fail"] += 1
            direct_probe_counter["results"].append(direct_probe)

        rounds.append(
            RoundResult(
                round_no=idx,
                timestamp_utc=utc_now_iso(),
                sync_ok=sync_ok,
                source_status=source_status,
                direct_probe=direct_probe,
                error=error,
            )
        )

    summary: Dict[str, dict] = {}
    for target_name, c in counters.items():
        ok = int(c["ok"])
        fail = int(c["fail"])
        ok_rate = (ok / round_count) if round_count else 0.0
        passed = fail == 0 and ok == round_count
        if target_name == DIRECT_PROBE_TARGET:
            passed = False
        summary[target_name] = {
            "sourceKey": TARGETS[target_name],
            "ok": ok,
            "fail": fail,
            "okRate": round(ok_rate, 4),
            "pass": passed,
            "statuses": c["statuses"],
        }

    direct_ok = int(direct_probe_counter["ok"])
    direct_fail = int(direct_probe_counter["fail"])
    summary[DIRECT_PROBE_TARGET]["directProbe"] = {
        "ok": direct_ok,
        "fail": direct_fail,
        "okRate": round((direct_ok / round_count) if round_count else 0.0, 4),
        "pass": direct_fail == 0 and direct_ok == round_count,
        "results": direct_probe_counter["results"],
    }
    summary[DIRECT_PROBE_TARGET]["pass"] = summary[DIRECT_PROBE_TARGET]["directProbe"]["pass"]

    return rounds, summary


def main() -> int:
    repo_root = Path(__file__).resolve().parents[1]
    logs_dir = repo_root / "logs"
    logs_dir.mkdir(parents=True, exist_ok=True)

    ts_local = datetime.now().strftime("%Y%m%d-%H%M%S")
    log_path = logs_dir / f"bkzj-smoke-{ts_local}.json"

    backend_process: Optional[subprocess.Popen] = None
    backend_started_here = False

    started_at = utc_now_iso()
    try:
        backend_process, backend_started_here = start_backend_if_needed(repo_root)
        rounds, summary = run_probe(ROUND_COUNT)
    except Exception as exc:  # noqa: BLE001
        payload = {
            "startedAt": started_at,
            "endedAt": utc_now_iso(),
            "baseUrl": BASE_URL,
            "roundCount": ROUND_COUNT,
            "backendStartedHere": backend_started_here,
            "fatalError": str(exc),
            "results": [],
            "summary": {},
            "overallPass": False,
        }
        log_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
        print(f"FATAL: {exc}")
        print(f"Log written: {log_path}")
        return 1
    finally:
        if backend_process is not None:
            terminate_process(backend_process)

    overall_pass = all(item.get("pass") is True for item in summary.values())

    print(f"Rounds: {ROUND_COUNT}")
    print(f"Base URL: {BASE_URL}")
    for target_name, result in summary.items():
        print(
            f"{target_name}: ok={result['ok']} fail={result['fail']} "
            f"ok_rate={result['okRate']:.2%} pass={result['pass']}"
        )
    print(f"Overall: {'PASS' if overall_pass else 'FAIL'}")

    payload = {
        "startedAt": started_at,
        "endedAt": utc_now_iso(),
        "baseUrl": BASE_URL,
        "roundCount": ROUND_COUNT,
        "backendStartedHere": backend_started_here,
        "results": [
            {
                "round": r.round_no,
                "timestampUtc": r.timestamp_utc,
                "syncOk": r.sync_ok,
                "sourceStatus": r.source_status,
                "directProbe": r.direct_probe,
                "error": r.error,
            }
            for r in rounds
        ],
        "summary": summary,
        "overallPass": overall_pass,
    }
    log_path.write_text(json.dumps(payload, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Log written: {log_path}")

    return 0 if overall_pass else 1


if __name__ == "__main__":
    sys.exit(main())
