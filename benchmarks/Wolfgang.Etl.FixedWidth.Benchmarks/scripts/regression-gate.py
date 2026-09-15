"""Absolute-floor benchmark-regression gate.

Ported from Chris-Wolfgang/ETL-Abstractions#367 / PR #400, after the
ratio-only gate false-positived on the Abstractions 0.23.2 bump here
(Chris-Wolfgang/ETL-Abstractions#427 — confirmed to be runner noise, not a
real regression, after re-running fresh).

The github-action-benchmark alert is ratio-only, so the smallest (sub-
millisecond) benchmarks trip a 1.50x ratio on runner noise alone and block
releases — the opposite of what the gate is for. A regression is only real
if it is BOTH ratio-significant AND materially large in absolute terms, so
this gate fails only when a benchmark's central value has grown by at least
`ratio` x AND by at least `floor_ns` nanoseconds. A 17 us noise blip on a
30 us benchmark clears the floor check; a genuine per-item regression (which
shows up at the large record counts too) does not.

Two baseline shapes are accepted (#355):

  * a gh-pages `data.js` (window.BENCHMARK_DATA = {...};) — the chart's latest
    main data point. It stores only the MEAN, so the comparison is mean vs
    mean. This is the first-sample gate: cheap, but exposed to runner-to-
    runner drift, so its alert is not trusted on its own.
  * a BenchmarkDotNet JSON report — a run of the merge-base on the SAME
    runner, immediately before the PR head. Both sides then carry the full
    statistics and the comparison is MEDIAN vs median: with --job short's
    three measured iterations a single stalled disk flush moves the mean by
    an order of magnitude but leaves the median untouched.

Usage:
    regression-gate.py <current-bdn-report.json> <baseline: data.js | bdn-report.json> <ratio> <floor_ns> [regressed-out]

`regressed-out`, when given, receives one BenchmarkDotNet --filter glob per
regressed METHOD (parameters stripped, deduplicated), so a confirm job can
re-run just the flagged benchmarks on both sides instead of the whole suite.

Exit codes: 0 = no real regression, 1 = at least one benchmark regressed past both
thresholds, 2 = usage/parse error.
"""

import json
import sys


def read_json_text(path):
    with open(path, encoding="utf-8") as f:
        return f.read()


def load_bdn_report(text, statistic):
    """BDN JSON report -> {full_name: statistic_ns}."""
    data = json.loads(text)
    return {b["FullName"]: float(b["Statistics"][statistic]) for b in data.get("Benchmarks", [])}


def load_chart_baseline(text):
    """gh-pages data.js (window.BENCHMARK_DATA = {...};) -> {name: mean_ns} for the latest entry."""
    text = text.strip()
    prefix = "window.BENCHMARK_DATA"
    if text.startswith(prefix):
        text = text[text.index("=", len(prefix)) + 1:]
    text = text.strip().rstrip(";").strip()
    data = json.loads(text)
    entries = data["entries"]["BenchmarkDotNet"]
    latest = entries[-1]["benches"]
    return {b["name"]: float(b["value"]) for b in latest}


def load_baseline(path):
    """Returns (values, statistic, description). A BDN report is compared by median; data.js by mean."""
    text = read_json_text(path)
    stripped = text.lstrip()
    if stripped.startswith("{"):
        try:
            data = json.loads(stripped)
        except json.JSONDecodeError:
            data = None
        if isinstance(data, dict) and "Benchmarks" in data:
            return load_bdn_report(stripped, "Median"), "Median", "same-runner BDN report (median vs median)"
    return load_chart_baseline(text), "Mean", "gh-pages chart baseline (mean vs mean; the chart stores no median)"


def method_glob(full_name):
    """'Ns.Class.Method(RecordCount: 1000)' -> '*Class.Method*' — one --filter glob per method."""
    without_params = full_name.split("(", 1)[0]
    parts = without_params.rsplit(".", 2)
    return "*" + ".".join(parts[-2:]) + "*"


def main(argv):
    if len(argv) not in (5, 6):
        print(__doc__)
        return 2
    baseline, statistic, description = load_baseline(argv[2])
    current = load_bdn_report(read_json_text(argv[1]), statistic)
    ratio_threshold = float(argv[3])
    floor_ns = float(argv[4])
    regressed_out = argv[5] if len(argv) == 6 else None

    regressions = []
    print(f"Baseline: {description}")
    print(f"Gate: fail only if ratio >= {ratio_threshold:.2f}x AND abs delta >= {floor_ns:,.0f} ns\n")
    print(f"{'benchmark':<70} {'current':>14} {'baseline':>14} {'ratio':>7} {'delta ns':>14}")
    for name, cur in sorted(current.items()):
        base = baseline.get(name)
        if base is None or base <= 0:
            continue
        ratio = cur / base
        delta = cur - base
        flag = ""
        if ratio >= ratio_threshold and delta >= floor_ns:
            flag = "  <-- REGRESSION (ratio AND floor)"
            regressions.append((name, ratio, delta))
        elif ratio >= ratio_threshold:
            flag = "  (ratio only - under floor, treated as noise)"
        short = name.split("(", 1)[0].rsplit(".", 2)
        short = ".".join(short[-2:]) + (("(" + name.split("(", 1)[1]) if "(" in name else "")
        print(f"{short:<70} {cur:>14,.0f} {base:>14,.0f} {ratio:>6.2f}x {delta:>14,.0f}{flag}")
    print()

    if regressed_out is not None:
        globs = sorted({method_glob(name) for name, _, _ in regressions})
        with open(regressed_out, "w", encoding="utf-8") as f:
            f.write("\n".join(globs) + ("\n" if globs else ""))
        print(f"Regressed method filters ({len(globs)}) written to {regressed_out}")

    if regressions:
        print(f"::error::{len(regressions)} benchmark(s) regressed past BOTH the ratio and the absolute floor.")
        return 1
    print("No benchmark cleared both the ratio and the absolute floor - gate passes.")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
