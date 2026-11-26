type: breaking

Analyzer rules `FW004` (no `Format`) and `FW005` (`Format` wider than the field) now also cover `DateOnly` and `TimeOnly` fields, so an existing `DateOnly` / `TimeOnly` field without a fitting `Format` starts warning (an error under `TreatWarningsAsErrors`); add a `Format` such as `yyyyMMdd` or `HHmmss`.
