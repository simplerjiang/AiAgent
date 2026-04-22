#!/usr/bin/env python3
from __future__ import annotations

import json
import random
import time
import zlib
import gzip
from dataclasses import dataclass
from datetime import datetime
from pathlib import Path
from typing import Any, Dict, List, Optional, Tuple
from urllib.error import HTTPError, URLError
from urllib.parse import urlencode
from urllib.request import Request, build_opener, HTTPCookieProcessor
import http.cookiejar as cookiejar

RANDOM_SEED = 20260417
ROUNDS = 5
BASE_TIMEOUTS = [3, 6, 10, 6, 10]
BASE_RETRIES = [1, 2, 3, 2, 1]
BACKOFF_SECONDS = 0.25

BOARD_CODES = ["m:90+s:4", "m:90+t:3", "m:90+t:1"]
BOARD_PAGE_SIZES = [50, 100, 200, 100, 200]
BOARD_KEY_COMBOS = [["f3", "f62"], ["f3"], ["f62"], ["f3", "f62"], ["f3", "f62"]]

TRADING_DATE = datetime.now().strftime("%Y%m%d")

UA_PROFILES = [
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Edg/135.0.0.0 Safari/537.36",
    "Mozilla/5.0 (Macintosh; Intel Mac OS X 14_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
    "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/135.0.0.0 Mobile Safari/537.36",
    "curl/8.7.1",
]

REFERERS = [
    "https://quote.eastmoney.com/",
    "https://data.eastmoney.com/",
    "https://data.10jqka.com.cn/",
    "",
]

ACCEPT_PROFILES = [
    {
        "Accept": "application/json, text/plain, */*",
        "Accept-Language": "zh-CN,zh;q=0.9,en;q=0.8",
        "Accept-Encoding": "gzip, deflate",
    },
    {
        "Accept": "*/*",
        "Accept-Language": "en-US,en;q=0.8",
        "Accept-Encoding": "gzip",
    },
    {
        "Accept": "application/json",
        "Accept-Language": "zh-CN",
        "Accept-Encoding": "identity",
    },
    {
        "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
        "Accept-Language": "zh-CN,zh;q=0.9",
        "Accept-Encoding": "gzip, deflate",
    },
    {
        "Accept": "application/json, */*;q=0.1",
        "Accept-Language": "en;q=0.7",
        "Accept-Encoding": "deflate",
    },
]

COOKIE_POLICIES = ["none", "warm_once", "session_persist", "none", "session_persist"]
CONNECTIONS = ["keep-alive", "keep-alive", "close", "close", "keep-alive"]
XRW_FLAGS = [False, True, False, True, False]


@dataclass
class ProbeResult:
    source: str
    round_no: int
    transport: bool
    payload: bool
    business: bool
    http_code: Optional[int]
    latency_ms: float
    detail: str
    profile: Dict[str, Any]


def decode_bytes(raw: bytes, encoding: str) -> str:
    enc = (encoding or "").lower().strip()
    try:
        if enc == "gzip":
            return gzip.decompress(raw).decode("utf-8", errors="replace")
        if enc == "deflate":
            return zlib.decompress(raw).decode("utf-8", errors="replace")
    except Exception:
        pass
    return raw.decode("utf-8", errors="replace")


def make_opener(cookie_policy: str, persist_jar: Optional[cookiejar.CookieJar]) -> Tuple[Any, Optional[cookiejar.CookieJar]]:
    if cookie_policy == "none":
        return build_opener(), None
    if cookie_policy == "session_persist":
        if persist_jar is None:
            persist_jar = cookiejar.CookieJar()
        return build_opener(HTTPCookieProcessor(persist_jar)), persist_jar
    jar = cookiejar.CookieJar()
    return build_opener(HTTPCookieProcessor(jar)), jar


def warm_cookie_if_needed(opener: Any, profile: Dict[str, Any], cookie_policy: str, timeout_s: int) -> None:
    if cookie_policy not in ("warm_once", "session_persist"):
        return
    target = "https://quote.eastmoney.com/"
    req = Request(target, method="GET")
    req.add_header("User-Agent", profile["ua"])
    req.add_header("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8")
    req.add_header("Accept-Language", profile["accept_language"])
    req.add_header("Connection", profile["connection"])
    try:
        with opener.open(req, timeout=timeout_s):
            return
    except Exception:
        return


def fetch_json(
    opener: Any,
    url: str,
    profile: Dict[str, Any],
    timeout_s: int,
    retries: int,
) -> Tuple[bool, Optional[int], Any, str, float]:
    start = time.perf_counter()
    last_err = ""
    for attempt in range(1, retries + 1):
        req = Request(url, method="GET")
        req.add_header("User-Agent", profile["ua"])
        req.add_header("Accept", profile["accept"])
        req.add_header("Accept-Language", profile["accept_language"])
        req.add_header("Accept-Encoding", profile["accept_encoding"])
        req.add_header("Connection", profile["connection"])
        if profile["referer"]:
            req.add_header("Referer", profile["referer"])
        if profile["xrw"]:
            req.add_header("X-Requested-With", "XMLHttpRequest")

        try:
            with opener.open(req, timeout=timeout_s) as resp:
                code = getattr(resp, "status", 200)
                raw = resp.read()
                text = decode_bytes(raw, resp.headers.get("Content-Encoding", ""))
                latency_ms = (time.perf_counter() - start) * 1000.0
                if "callback" in text and "(" in text and ")" in text and "{" in text:
                    left = text.find("(")
                    right = text.rfind(")")
                    if left >= 0 and right > left:
                        text = text[left + 1:right]
                parsed = json.loads(text)
                return True, code, parsed, "", latency_ms
        except HTTPError as exc:
            last_err = f"HTTPError {exc.code}: {exc.reason}"
            if exc.code >= 500 and attempt < retries:
                time.sleep(BACKOFF_SECONDS * attempt)
                continue
            break
        except URLError as exc:
            last_err = f"URLError: {exc.reason}"
            if attempt < retries:
                time.sleep(BACKOFF_SECONDS * attempt)
                continue
            break
        except json.JSONDecodeError as exc:
            last_err = f"JSONDecodeError: {exc}"
            break
        except Exception as exc:
            last_err = f"Exception: {exc}"
            if attempt < retries:
                time.sleep(BACKOFF_SECONDS * attempt)
                continue
            break

    latency_ms = (time.perf_counter() - start) * 1000.0
    return False, None, None, last_err, latency_ms


def list_rows(payload: Any) -> List[Dict[str, Any]]:
    if not isinstance(payload, dict):
        return []
    data = payload.get("data")
    if isinstance(data, dict) and isinstance(data.get("diff"), list):
        return [r for r in data.get("diff", []) if isinstance(r, dict)]
    if isinstance(data, list):
        return [r for r in data if isinstance(r, dict)]
    if isinstance(payload.get("result"), dict) and isinstance(payload["result"].get("data"), list):
        return [r for r in payload["result"].get("data", []) if isinstance(r, dict)]
    return []


def probe_board_bkzj(opener: Any, profile: Dict[str, Any], timeout_s: int, retries: int, key_combo: List[str], page_size: int) -> Tuple[bool, bool, str]:
    all_rows: Dict[str, Dict[str, Dict[str, Any]]] = {}
    transport_ok = True
    payload_ok = True

    for code in BOARD_CODES:
        all_rows[code] = {}
        for key in key_combo:
            params = {
                "sortField": key,
                "sortDirec": "1",
                "pageNum": "1",
                "pageSize": str(page_size),
                "code": code,
                "key": key,
            }
            url = "https://data.eastmoney.com/dataapi/bkzj/getbkzj?" + urlencode(params)
            ok, _, payload, err, _ = fetch_json(opener, url, profile, timeout_s, retries)
            if not ok:
                transport_ok = False
                payload_ok = False
                return transport_ok, payload_ok, f"bkzj request failed for {code}/{key}: {err}"
            rows = list_rows(payload)
            if not rows:
                payload_ok = False
                return transport_ok, payload_ok, f"empty rows for {code}/{key}"
            all_rows[code][key] = {str(r.get("f12", "")): r for r in rows}

    board_valid = 0
    board_total = 0
    for code in BOARD_CODES:
        for sector_code in set().union(*[set(all_rows[code][k].keys()) for k in key_combo]):
            board_total += 1
            row_ok = True
            code_ok = sector_code.startswith("BK")
            row_ok = row_ok and code_ok
            name_any = None
            if "f3" in key_combo and sector_code in all_rows[code].get("f3", {}):
                name_any = all_rows[code]["f3"][sector_code].get("f14")
                try:
                    float(all_rows[code]["f3"][sector_code].get("f3", 0))
                except Exception:
                    row_ok = False
            if "f62" in key_combo and sector_code in all_rows[code].get("f62", {}):
                name_any = name_any or all_rows[code]["f62"][sector_code].get("f14")
                try:
                    float(all_rows[code]["f62"][sector_code].get("f62", 0))
                except Exception:
                    row_ok = False
            if not name_any:
                row_ok = False
            if row_ok:
                board_valid += 1

    if board_total == 0:
        return transport_ok, payload_ok, "board_total=0"

    ratio = board_valid / float(board_total)
    dual_required = ("f3" in key_combo and "f62" in key_combo)
    business_ok = ratio >= 0.8 and (not dual_required or len(key_combo) == 2)
    return transport_ok, payload_ok and board_total > 0, f"board_valid_ratio={ratio:.3f};dual={dual_required};business={business_ok}"


def probe_turnover_ulist(opener: Any, profile: Dict[str, Any], timeout_s: int, retries: int) -> Tuple[bool, bool, bool, str]:
    fields = "f12,f13,f14,f2,f3,f4,f5,f6,f8,f9,f10,f15,f16,f124,f152"
    secids = "1.000001,0.399001"
    url = f"https://push2.eastmoney.com/api/qt/ulist.np/get?fltt=2&fields={fields}&secids={secids}"
    ok, _, payload, err, _ = fetch_json(opener, url, profile, timeout_s, retries)
    if not ok:
        return False, False, False, err
    rows = list_rows(payload)
    if len(rows) < 2:
        return True, False, False, "rows<2"
    total = 0.0
    valid = 0
    for r in rows:
        try:
            total += max(0.0, float(r.get("f6") or 0.0))
            valid += 1
        except Exception:
            continue
    payload_ok = valid >= 2
    business_ok = payload_ok and total > 0
    return True, payload_ok, business_ok, f"rows={len(rows)};valid={valid};total={total:.2f}"


def probe_breadth_clist(opener: Any, profile: Dict[str, Any], timeout_s: int, retries: int, descending: bool) -> Tuple[bool, bool, bool, str]:
    fs = "m:0+t:6,m:0+t:80,m:1+t:2,m:1+t:23"
    po = 1 if descending else 0
    url = f"https://push2.eastmoney.com/api/qt/clist/get?pn=1&pz=100&po={po}&np=1&fltt=2&invt=2&fid=f3&fs={fs}&fields=f12,f3,f6"
    ok, _, payload, err, _ = fetch_json(opener, url, profile, timeout_s, retries)
    if not ok:
        return False, False, False, err
    rows = list_rows(payload)
    if not rows:
        return True, False, False, "empty rows"
    adv = dec = flat = 0
    turnover = 0.0
    valid = 0
    for r in rows:
        try:
            chg = float(r.get("f3") or 0.0)
            amt = float(r.get("f6") or 0.0)
            valid += 1
            turnover += max(0.0, amt)
            if chg > 0:
                adv += 1
            elif chg < 0:
                dec += 1
            else:
                flat += 1
        except Exception:
            continue
    payload_ok = valid > 0
    business_ok = payload_ok and (adv + dec + flat) >= 50
    return True, payload_ok, business_ok, f"valid={valid};adv={adv};dec={dec};flat={flat};turnover={turnover:.2f}"


def probe_breadth_push2ex(opener: Any, profile: Dict[str, Any], timeout_s: int, retries: int) -> Tuple[bool, bool, bool, str]:
    url = "https://push2ex.eastmoney.com/getTopicZDFenBu?cb=callbackdata7930743&ut=7eea3edcaed734bea9cbfc24409ed989&dpt=wz.ztzt"
    ok, _, payload, err, _ = fetch_json(opener, url, profile, timeout_s, retries)
    if not ok:
        return False, False, False, err
    data = payload.get("data") if isinstance(payload, dict) else None
    fenbu = data.get("fenbu") if isinstance(data, dict) else None
    if not isinstance(fenbu, list) or not fenbu:
        return True, False, False, "fenbu missing"
    count = 0
    for obj in fenbu:
        if isinstance(obj, dict):
            count += sum(int(v) for v in obj.values() if isinstance(v, (int, float)))
    payload_ok = count > 0
    business_ok = payload_ok and count >= 100
    return True, payload_ok, business_ok, f"bucket_sum={count}"


def probe_maxstreak_ths(opener: Any, profile: Dict[str, Any], timeout_s: int, retries: int) -> Tuple[bool, bool, bool, str]:
    url = f"https://data.10jqka.com.cn/dataapi/limit_up/continuous_limit_up?date={TRADING_DATE}&page=1&limit=100"
    ok, _, payload, err, _ = fetch_json(opener, url, profile, timeout_s, retries)
    if not ok:
        return False, False, False, err
    data = payload.get("data") if isinstance(payload, dict) else None
    if not isinstance(data, list):
        return True, False, False, "data missing"
    heights: List[int] = []
    for row in data:
        if isinstance(row, dict):
            try:
                heights.append(int(row.get("height") or 0))
            except Exception:
                continue
    payload_ok = len(data) > 0
    business_ok = payload_ok and len(heights) > 0 and max(heights) >= 0
    return True, payload_ok, business_ok, f"rows={len(data)};max_height={(max(heights) if heights else -1)}"


def probe_maxstreak_ztpool(opener: Any, profile: Dict[str, Any], timeout_s: int, retries: int) -> Tuple[bool, bool, bool, str]:
    url = f"https://push2ex.eastmoney.com/getTopicZTPool?ut=7eea3edcaed734bea9cbfc24409ed989&dpt=wz.ztzt&Pageindex=0&pagesize=1000&date={TRADING_DATE}"
    ok, _, payload, err, _ = fetch_json(opener, url, profile, timeout_s, retries)
    if not ok:
        return False, False, False, err
    data = payload.get("data") if isinstance(payload, dict) else None
    pool = data.get("pool") if isinstance(data, dict) else None
    if not isinstance(pool, list):
        rc = payload.get("rc") if isinstance(payload, dict) else None
        return True, False, False, f"pool missing;rc={rc}"
    lbc_max = 0
    for row in pool:
        if isinstance(row, dict):
            try:
                lbc_max = max(lbc_max, int(row.get("lbc") or 0))
            except Exception:
                continue
    payload_ok = len(pool) > 0
    business_ok = payload_ok and lbc_max >= 0
    return True, payload_ok, business_ok, f"pool={len(pool)};lbc_max={lbc_max}"


def probe_datacenter_limit_pool(opener: Any, profile: Dict[str, Any], timeout_s: int, retries: int) -> Tuple[bool, bool, bool, str]:
    params = {
        "sortColumns": "SECURITY_CODE",
        "sortTypes": "1",
        "pageSize": "50",
        "pageNumber": "1",
        "reportName": "RPT_LIMIT_UP_POOL",
        "columns": "ALL",
    }
    url = "https://datacenter-web.eastmoney.com/api/data/v1/get?" + urlencode(params)
    ok, _, payload, err, _ = fetch_json(opener, url, profile, timeout_s, retries)
    if not ok:
        return False, False, False, err
    success = payload.get("success") if isinstance(payload, dict) else None
    result = payload.get("result") if isinstance(payload, dict) else None
    rows = result.get("data") if isinstance(result, dict) else None
    payload_ok = bool(success) and isinstance(rows, list) and len(rows) > 0
    business_ok = payload_ok
    code = payload.get("code") if isinstance(payload, dict) else None
    msg = payload.get("message") if isinstance(payload, dict) else None
    return True, payload_ok, business_ok, f"success={success};code={code};msg={msg};rows={(len(rows) if isinstance(rows, list) else 0)}"


def build_profile(round_no: int) -> Dict[str, Any]:
    idx = (round_no - 1) % 5
    accept_block = ACCEPT_PROFILES[idx]
    return {
        "ua": UA_PROFILES[idx],
        "referer": REFERERS[idx % len(REFERERS)],
        "accept": accept_block["Accept"],
        "accept_language": accept_block["Accept-Language"],
        "accept_encoding": accept_block["Accept-Encoding"],
        "connection": CONNECTIONS[idx],
        "xrw": XRW_FLAGS[idx],
        "cookie_policy": COOKIE_POLICIES[idx],
        "timeout": BASE_TIMEOUTS[idx],
        "retries": BASE_RETRIES[idx],
        "board_page_size": BOARD_PAGE_SIZES[idx],
        "board_key_combo": BOARD_KEY_COMBOS[idx],
        "clist_descending": idx % 2 == 0,
    }


def run() -> Dict[str, Any]:
    random.seed(RANDOM_SEED)
    started = datetime.now().isoformat()
    results: List[ProbeResult] = []

    session_jar: Optional[cookiejar.CookieJar] = None

    for round_no in range(1, ROUNDS + 1):
        profile = build_profile(round_no)
        opener, session_jar = make_opener(profile["cookie_policy"], session_jar)
        warm_cookie_if_needed(opener, profile, profile["cookie_policy"], profile["timeout"])

        def add(source: str, transport: bool, payload: bool, business: bool, code: Optional[int], latency: float, detail: str) -> None:
            results.append(
                ProbeResult(
                    source=source,
                    round_no=round_no,
                    transport=transport,
                    payload=payload,
                    business=business,
                    http_code=code,
                    latency_ms=latency,
                    detail=detail,
                    profile={
                        "ua": profile["ua"],
                        "referer": profile["referer"],
                        "accept": profile["accept"],
                        "accept_encoding": profile["accept_encoding"],
                        "accept_language": profile["accept_language"],
                        "connection": profile["connection"],
                        "x_requested_with": profile["xrw"],
                        "cookie_policy": profile["cookie_policy"],
                        "timeout": profile["timeout"],
                        "retries": profile["retries"],
                        "board_page_size": profile["board_page_size"],
                        "board_key_combo": profile["board_key_combo"],
                    },
                )
            )

        t0 = time.perf_counter()
        tr, pr, detail = probe_board_bkzj(opener, profile, profile["timeout"], profile["retries"], ["f3"], profile["board_page_size"])
        add("board_bkzj_f3_only", tr, pr, pr and "business=True" in detail, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, detail = probe_board_bkzj(opener, profile, profile["timeout"], profile["retries"], ["f62"], profile["board_page_size"])
        add("board_bkzj_f62_only", tr, pr, pr and "business=True" in detail, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, detail = probe_board_bkzj(opener, profile, profile["timeout"], profile["retries"], profile["board_key_combo"], profile["board_page_size"])
        add("board_bkzj_dual_merge", tr, pr, pr and "business=True" in detail, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, br, detail = probe_turnover_ulist(opener, profile, profile["timeout"], profile["retries"])
        add("turnover_push2_ulist", tr, pr, br, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, br, detail = probe_breadth_clist(opener, profile, profile["timeout"], profile["retries"], profile["clist_descending"])
        add("breadth_push2_clist", tr, pr, br, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, br, detail = probe_breadth_push2ex(opener, profile, profile["timeout"], profile["retries"])
        add("breadth_push2ex_zdfenbu", tr, pr, br, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, br, detail = probe_maxstreak_ths(opener, profile, profile["timeout"], profile["retries"])
        add("maxstreak_ths_continuous", tr, pr, br, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, br, detail = probe_maxstreak_ztpool(opener, profile, profile["timeout"], profile["retries"])
        add("maxstreak_push2ex_ztpool", tr, pr, br, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

        t0 = time.perf_counter()
        tr, pr, br, detail = probe_datacenter_limit_pool(opener, profile, profile["timeout"], profile["retries"])
        add("maxstreak_datacenter_limit_pool", tr, pr, br, 200 if tr else None, (time.perf_counter() - t0) * 1000.0, detail)

    by_source: Dict[str, Dict[str, Any]] = {}
    for row in results:
        item = by_source.setdefault(
            row.source,
            {
                "rounds": 0,
                "transport_ok": 0,
                "payload_ok": 0,
                "business_ok": 0,
                "latency_ms": [],
                "samples": [],
            },
        )
        item["rounds"] += 1
        item["transport_ok"] += int(row.transport)
        item["payload_ok"] += int(row.payload)
        item["business_ok"] += int(row.business)
        item["latency_ms"].append(row.latency_ms)
        if len(item["samples"]) < 3:
            item["samples"].append(
                {
                    "round": row.round_no,
                    "detail": row.detail,
                    "profile": row.profile,
                }
            )

    for item in by_source.values():
        lats = item["latency_ms"]
        item["latency_avg_ms"] = round(sum(lats) / len(lats), 2) if lats else None
        item["transport_rate"] = round(item["transport_ok"] / item["rounds"], 3)
        item["payload_rate"] = round(item["payload_ok"] / item["rounds"], 3)
        item["business_rate"] = round(item["business_ok"] / item["rounds"], 3)
        del item["latency_ms"]

    ended = datetime.now().isoformat()
    return {
        "started": started,
        "ended": ended,
        "rounds": ROUNDS,
        "dimensions": {
            "ua_profiles": UA_PROFILES,
            "referers": REFERERS,
            "accept_profiles": ACCEPT_PROFILES,
            "cookie_policies": COOKIE_POLICIES,
            "connection_modes": CONNECTIONS,
            "xrw_flags": XRW_FLAGS,
            "timeouts": BASE_TIMEOUTS,
            "retries": BASE_RETRIES,
            "board_codes": BOARD_CODES,
            "board_page_sizes": BOARD_PAGE_SIZES,
            "board_key_combos": BOARD_KEY_COMBOS,
        },
        "summary": by_source,
    }


def main() -> int:
    report = run()
    repo = Path(__file__).resolve().parents[1]
    logs = repo / "logs"
    logs.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now().strftime("%Y%m%d-%H%M%S")
    output = logs / f"market-reachability-matrix-{stamp}.json"
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")

    print("=== market reachability matrix summary ===")
    for name in sorted(report["summary"].keys()):
        item = report["summary"][name]
        print(
            f"{name}: transport={item['transport_ok']}/{item['rounds']} payload={item['payload_ok']}/{item['rounds']} "
            f"business={item['business_ok']}/{item['rounds']} avg_ms={item['latency_avg_ms']}"
        )
    print(f"report={output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
