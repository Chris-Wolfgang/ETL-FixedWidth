# 0001. Record architecture decisions

- **Status**: accepted
- **Date**: 2026-07-16

## Context

Non-obvious design choices — why the numeric parser defaults to a per-type
`NumberStyles`, why the extractor splits its counters four ways, why line-ending
control is a documentation pattern rather than an enum — get lost six months
after the PR that introduced them. Future maintainers re-derive the same
trade-offs, often worse, or unknowingly reverse them.

## Decision

We keep Architecture Decision Records (ADRs) — short markdown documents capturing
the context, decision, and consequences of each non-obvious choice — in
`docs/adr/`, numbered sequentially, using the [TEMPLATE.md](TEMPLATE.md) shape
(a trimmed Michael Nygard template). A new ADR lands alongside the PR that makes
the decision; superseding decisions link back to the ADR they replace rather than
editing history.

## Consequences

- The reasoning behind a choice survives independently of the PR discussion that
  produced it.
- There is a small per-decision authoring cost; it applies only to *non-obvious*
  choices, not routine changes.
- Retroactive ADRs (0002–0004) capture decisions that predate this record.
