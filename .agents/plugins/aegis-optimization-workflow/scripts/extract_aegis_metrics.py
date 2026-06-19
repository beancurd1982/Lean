#!/usr/bin/env python3
"""Extract normalized Aegis metrics from a QuantConnect result JSON."""

from __future__ import annotations

import argparse
import json
import sys
from decimal import Decimal, InvalidOperation
from pathlib import Path
from typing import Any


METRIC_ALIASES = {
    "start_equity": ("start equity", "startEquity"),
    "end_equity": ("end equity", "endEquity"),
    "net_profit": ("net profit", "total net profit", "totalNetProfit"),
    "cagr": ("compounding annual return", "compoundingAnnualReturn"),
    "drawdown": ("drawdown",),
    "sharpe": ("sharpe ratio", "sharpeRatio"),
    "psr": ("probabilistic sharpe ratio", "probabilisticSharpeRatio"),
    "turnover": ("portfolio turnover", "portfolioTurnover"),
    "fees": ("total fees", "totalFees"),
    "orders": ("total orders", "orders"),
    "trades": ("total trades", "totalNumberOfTrades"),
    "win_rate": ("win rate", "winRate"),
    "profit_factor": ("profit factor", "profitFactor"),
}


def normalize_key(value: str) -> str:
    return "".join(char.lower() for char in value if char.isalnum())


def to_decimal(value: Any) -> Decimal | None:
    if value is None:
        return None
    if isinstance(value, bool):
        return None
    try:
        cleaned = (
            str(value)
            .replace("%", "")
            .replace(",", "")
            .replace("$", "")
            .replace(" ", "")
            .strip()
        )
        return Decimal(cleaned)
    except (InvalidOperation, ValueError):
        return None


def flatten_dict(node: Any, output: dict[str, Any]) -> None:
    if isinstance(node, dict):
        for key, value in node.items():
            output.setdefault(normalize_key(str(key)), value)
            flatten_dict(value, output)
    elif isinstance(node, list):
        for item in node:
            flatten_dict(item, output)


def latest_rolling_window(data: dict[str, Any]) -> dict[str, Any] | None:
    window = data.get("rollingWindow")
    if not isinstance(window, dict) or not window:
        return None
    latest_key = sorted(window.keys())[-1]
    latest = window.get(latest_key)
    return latest if isinstance(latest, dict) else None


def find_metrics(data: dict[str, Any]) -> dict[str, Any]:
    search: dict[str, Any] = {}
    for key in ("statistics", "runtimeStatistics"):
        node = data.get(key)
        if isinstance(node, dict):
            flatten_dict(node, search)

    total_performance = data.get("totalPerformance")
    if isinstance(total_performance, dict):
        flatten_dict(total_performance.get("portfolioStatistics"), search)
        flatten_dict(total_performance.get("tradeStatistics"), search)

    for key, value in data.items():
        if key not in {"rollingWindow", "statistics", "runtimeStatistics", "totalPerformance"}:
            flatten_dict(value, search)

    latest = latest_rolling_window(data)
    if latest is not None:
        for key, value in latest.items():
            flattened: dict[str, Any] = {}
            flatten_dict(value, flattened)
            for flattened_key, flattened_value in flattened.items():
                search.setdefault(flattened_key, flattened_value)

    metrics: dict[str, Any] = {}
    for target, aliases in METRIC_ALIASES.items():
        for alias in aliases:
            value = search.get(normalize_key(alias))
            if value is None:
                continue
            decimal = to_decimal(value)
            if decimal is not None:
                metrics[target] = float(decimal)
                break

    if "start_equity" in metrics and "end_equity" in metrics and "net_profit" not in metrics:
        start = Decimal(str(metrics["start_equity"]))
        end = Decimal(str(metrics["end_equity"]))
        if start != 0:
            metrics["net_profit"] = float((end - start) / start)

    return metrics


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("result_json", type=Path)
    args = parser.parse_args()

    try:
        data = json.loads(args.result_json.read_text(encoding="utf-8-sig"))
    except Exception as exc:
        print(f"error: failed to read {args.result_json}: {exc}", file=sys.stderr)
        return 2

    metrics = find_metrics(data)
    if not metrics:
        print(f"error: no known metrics found in {args.result_json}", file=sys.stderr)
        return 1

    print(json.dumps({"source": str(args.result_json), "metrics": metrics}, indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
