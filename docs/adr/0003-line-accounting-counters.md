# 0003. Four-way line accounting: items / skipped / rejected / filtered

- **Status**: accepted
- **Date**: 2026-07-16

## Context

The extractor drops input lines for several distinct reasons, and consumers need
to tell them apart: a line intentionally paged over (`SkipItemCount`), a line the
caller's `RecordValidator` rejected, a structurally malformed line, a blank line,
and a line removed by a `LineFilter`. Collapsing all of these into a single
"skipped" count loses the signal a consumer needs to distinguish "the file had
bad data" from "I asked to skip these".

## Decision

The extractor exposes four counters that partition every physical line:

- `CurrentItemCount` — lines successfully yielded as records.
- `CurrentSkippedItemCount` — lines skipped by the `SkipItemCount` pagination budget.
- `CurrentRejectedItemCount` — lines that *looked like* records but failed:
  malformed layout, or a `RecordValidator` returning `Skip`/`Stop`.
- `CurrentFilteredLineCount` — lines that were never candidate records: blank
  lines under `BlankLineHandling.Skip`, `LineFilter` removals, and structural
  early-stops.

These close exactly:
`CurrentLineNumber == Items + Skipped + Rejected + Filtered`.

## Consequences

- `LineFilter` removals stay "invisible" as data (they are filtered, not
  rejected), yet `CurrentLineNumber` still reflects the true physical position in
  the source.
- The closed identity is a testable invariant (see `FixedWidthLineAccountingTests`).
- Adding a new drop reason in future requires deciding which of the four buckets
  it belongs to — rejected (was-a-candidate) vs filtered (never-a-candidate) — so
  the identity keeps closing.
