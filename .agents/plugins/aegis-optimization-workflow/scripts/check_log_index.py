#!/usr/bin/env python3
"""Validate the Aegis BackTestLogs log-index.csv without modifying it."""

from __future__ import annotations

import argparse
import csv
import sys
from collections import Counter
from pathlib import Path

REQUIRED_COLUMNS = {"processed_at", "created_at", "original_name", "normalized_name", "status"}
REVIEW_COLUMNS = ("review_ref", "review_note")
VALID_STATUSES = {"unreviewed", "reviewed"}


def review_value(row: dict[str, str]) -> str:
    for column in REVIEW_COLUMNS:
        value = row.get(column, "").strip()
        if value:
            return value
    return ""


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("log_index", type=Path)
    args = parser.parse_args()

    if not args.log_index.exists():
        print(f"error: missing log index {args.log_index}", file=sys.stderr)
        return 2

    with args.log_index.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        fieldnames = set(reader.fieldnames or [])
        missing_columns = sorted(REQUIRED_COLUMNS - fieldnames)
        if missing_columns:
            print(f"error: missing columns: {', '.join(missing_columns)}", file=sys.stderr)
            return 2
        rows = list(reader)

    errors: list[str] = []
    warnings: list[str] = []
    root = args.log_index.parent
    names = [row.get("normalized_name", "").strip() for row in rows]

    for name, count in Counter(names).items():
        if name and count > 1:
            errors.append(f"duplicate normalized_name: {name}")

    for index, row in enumerate(rows, start=2):
        name = row.get("normalized_name", "").strip()
        status = row.get("status", "").strip()
        review = review_value(row)

        if status not in VALID_STATUSES:
            errors.append(f"line {index}: invalid status {status!r}")

        if name and not (root / name).exists():
            warnings.append(f"line {index}: normalized file missing: {name}")

        if status == "reviewed" and not review:
            errors.append(f"line {index}: reviewed row has no review reference")

        if review and not (review.startswith("inline:") or review.startswith("project-notes/")):
            errors.append(f"line {index}: malformed review reference: {review}")

    unreviewed = sum(1 for row in rows if row.get("status", "").strip() == "unreviewed")
    print(f"rows: {len(rows)}")
    print(f"unreviewed: {unreviewed}")
    print(f"errors: {len(errors)}")
    print(f"warnings: {len(warnings)}")

    for item in errors:
        print(f"ERROR: {item}")
    for item in warnings:
        print(f"WARN: {item}")

    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
