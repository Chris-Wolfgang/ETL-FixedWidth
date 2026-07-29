#!/usr/bin/env python3
"""Gate shadow-workload results against the committed baseline (#140).

Usage: compare.py <run.json> <baseline.json>

allocatedBytes is the hard gate (deterministic): a per-scenario allocation jump
beyond allocTolerancePercent fails (exit 1). medianMs is advisory: wall-clock on
shared runners is too noisy to gate on, so a latency past latencyTolerancePercent
is flagged but does not fail the job. A new scenario missing from the baseline
also fails (the baseline must be updated deliberately).
"""
import json
import sys


def main() -> int:
    if len(sys.argv) != 3:
        print("usage: compare.py <run.json> <baseline.json>", file=sys.stderr)
        return 2

    with open(sys.argv[1], encoding="utf-8-sig") as f:
        run = json.load(f)
    with open(sys.argv[2], encoding="utf-8-sig") as f:
        base = json.load(f)

    alloc_tol = 1.0 + float(base["allocTolerancePercent"]) / 100.0
    lat_tol = 1.0 + float(base["latencyTolerancePercent"]) / 100.0
    baselines = base["scenarios"]

    failures = []
    for row in run:
        name = row["scenario"]
        if name not in baselines:
            failures.append(f"{name}: no baseline entry (update docs/shadow-baseline.json)")
            print(f"{name}: NO BASELINE")
            continue

        b = baselines[name]
        alloc = float(row["allocatedBytes"])
        alloc_ceiling = float(b["allocatedBytes"]) * alloc_tol
        alloc_ok = alloc <= alloc_ceiling

        ms = float(row["medianMs"])
        ms_ceiling = float(b["medianMs"]) * lat_tol
        ms_flag = "" if ms <= ms_ceiling else "  [latency advisory: over threshold]"

        status = "OK" if alloc_ok else "ALLOC REGRESSION"
        print(f"{name}: alloc {alloc:,.0f} (ceiling {alloc_ceiling:,.0f}) {status}; "
              f"median {ms:.2f}ms (baseline {float(b['medianMs']):.2f}ms){ms_flag}")

        if not alloc_ok:
            failures.append(f"{name}: allocatedBytes {alloc:,.0f} > ceiling {alloc_ceiling:,.0f}")

    if failures:
        print("\nSHADOW REGRESSION:\n  - " + "\n  - ".join(failures), file=sys.stderr)
        return 1

    print("\nAll scenarios within the allocation baseline.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
