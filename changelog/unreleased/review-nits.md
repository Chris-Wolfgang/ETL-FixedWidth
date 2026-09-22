type: internal

Review polish: `TryParseLine` declares its nullable out-parameter with `[MaybeNullWhen(false)]`, member spacing is normalised, and binary Text fields now encode non-string property values with the invariant culture (they previously used the current culture, so a `decimal` in a Text field could carry a locale-specific separator into the record).
