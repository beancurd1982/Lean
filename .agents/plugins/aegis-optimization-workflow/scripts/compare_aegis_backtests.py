#!/usr/bin/env python3
"""Compare two Aegis QuantConnect backtest result artifacts."""

from __future__ import annotations

import argparse
import csv
import json
import subprocess
import sys
from pathlib import Path
from typing import Any

SCRIPT_DIR = Path(__file__).resolve().parent
EXTRACTOR = SCRIPT_DIR / "extract_aegis_metrics.py"

REQUIRED = ("net_profit", "drawdown", "sharpe", "psr", "orders", "win_rate", "profit_factor")
LOWER_IS_BETTER = {"drawdown", "turnover", "fees", "orders"}


def load_metrics(path: Path) -> dict[str, float]:
    result = subprocess.run(
        [sys.executable, str(EXTRACTOR), str(path)],
        check=False,
        text=True,
        capture_output=True,
    )
    if result.returncode != 0:
        raise RuntimeError(result.stderr.strip() or result.stdout.strip())
    return json.loads(result.stdout)["metrics"]


def read_order_types(path: Path | None) -> set[str]:
    if path is None:
        return set()
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        if "Type" not in (reader.fieldnames or []):
            raise RuntimeError(f"{path} is missing a Type column")
        return {row["Type"].strip() for row in reader if row.get("Type")}


def metric_status(metric: str, baseline: float, candidate: float) -> str:
    if metric in LOWER_IS_BETTER:
        return "PASS" if candidate <= baseline else "FAIL"
    return "PASS" if candidate >= baseline else "FAIL"


def format_value(value: Any) -> str:
    if isinstance(value, float):
        return f"{value:.6g}"
    return str(value)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("baseline_json", type=Path)
    parser.add_argument("candidate_json", type=Path)
    parser.add_argument("--baseline-orders", type=Path)
    parser.add_argument("--candidate-orders", type=Path)
    args = parser.parse_args()

    try:
        baseline_orders = read_order_types(args.baseline_orders)
        candidate_orders = read_order_types(args.candidate_orders)
        if baseline_orders or candidate_orders:
            if baseline_orders != candidate_orders:
                raise RuntimeError(
                    f"execution model differs: baseline={sorted(baseline_orders)} candidate={sorted(candidate_orders)}"
                )

        baseline = load_metrics(args.baseline_json)
        candidate = load_metrics(args.candidate_json)
    except Exception as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 2

    missing = [name for name in REQUIRED if name not in baseline or name not in candidate]
    if missing:
        print(f"error: missing required metrics: {', '.join(missing)}", file=sys.stderr)
        return 1

    print("| Metric | Baseline | Candidate | Gate |")
    print("|---|---:|---:|---|")
    failures = []
    for metric in sorted(set(baseline) | set(candidate)):
        if metric not in baseline or metric not in candidate:
            continue
        status = metric_status(metric, baseline[metric], candidate[metric])
        if metric in REQUIRED and status == "FAIL":
            failures.append(metric)
        print(f"| {metric} | {format_value(baseline[metric])} | {format_value(candidate[metric])} | {status} |")

    if failures:
        print(f"\nDecision: FAIL stress gate versus baseline ({', '.join(failures)}).")
        return 1

    print("\nDecision: PASS measured gates versus baseline.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
