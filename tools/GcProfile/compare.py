#!/usr/bin/env python3
"""Gate a sustained-load GC run against the committed baseline (#152).

Usage: compare.py <run.json> <baseline.json>

Fails (exit 1) when the per-record allocation exceeds the baseline reference by
more than its tolerance, or when gen2 collections per million records exceed the
baseline max (a retention leak). Prints a summary either way.
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

    failures = []

    per_record = float(run["allocatedBytesPerRecord"])
    ref = float(base["allocatedBytesPerRecord"]["reference"])
    tol = float(base["allocatedBytesPerRecord"]["tolerancePercent"]) / 100.0
    ceiling = ref * (1.0 + tol)
    ok = per_record <= ceiling
    print(f"allocatedBytesPerRecord: {per_record:.1f}  (baseline {ref:.1f}, ceiling {ceiling:.1f})  {'OK' if ok else 'REGRESSION'}")
    if not ok:
        failures.append(f"allocatedBytesPerRecord {per_record:.1f} > ceiling {ceiling:.1f}")

    gen2 = float(run["gen2CollectionsPerMillion"])
    gen2_max = float(base["gen2CollectionsPerMillion"]["max"])
    ok = gen2 <= gen2_max
    print(f"gen2CollectionsPerMillion: {gen2:.3f}  (max {gen2_max:.3f})  {'OK' if ok else 'LEAK'}")
    if not ok:
        failures.append(f"gen2CollectionsPerMillion {gen2:.3f} > max {gen2_max:.3f}")

    print(f"recordsProcessed: {run.get('recordsProcessed')}  recordsPerSecond: {run.get('recordsPerSecond')}")

    if failures:
        print("\nGC REGRESSION:\n  - " + "\n  - ".join(failures), file=sys.stderr)
        return 1

    print("\nGC profile within baseline.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
