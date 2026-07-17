# 0002. Per-type default NumberStyles for field parsing

- **Status**: accepted
- **Date**: 2026-07-16

## Context

`[FixedWidthField]` gained a `NumberStyles` property (#9) so callers can control
how numeric fields are parsed. The question was what the *default* should be when
the caller does not specify one. `NumberStyles.Any` is permissive (accepts
currency symbols, parentheses-for-negative, exponents, thousands) but silently
masks malformed data; `NumberStyles.None` is stricter than the BCL's own
`int.Parse(string)` / `decimal.Parse(string)` and would surprise callers.

An attribute argument cannot be a nullable enum (CS0655), so "unset" cannot be
represented as `null` on the attribute itself.

## Decision

The default mirrors the BCL's own per-type parse defaults: `NumberStyles.Integer`
for integral types and `NumberStyles.Number` for `decimal` / `double` / `float`
(resolved in `FixedWidthConverter.ResolveNumberStyles`). The permissive forms
(currency, parentheses, exponent) must be opted into explicitly with
`NumberStyles = NumberStyles.Any`.

"Unset" is encoded as the sentinel `(NumberStyles)(-1)`
(`FixedWidthFieldAttribute.UnspecifiedNumberStyles`); `FieldDescriptor` maps the
sentinel to a nullable `NumberStyles?` in the parsing context, and a null there
selects the per-type default.

## Consequences

- Parsing behaviour matches what a developer expects from `int.Parse` /
  `decimal.Parse` with no configuration — a plain integer field rejects a decimal
  point; a decimal field accepts a decimal point and thousands separators.
- Malformed data is rejected by default rather than coerced, surfacing as a
  rejected line (see [ADR-0003](0003-line-accounting-counters.md)).
- The sentinel is an internal implementation detail; the public surface is the
  ordinary non-nullable `NumberStyles` property.
