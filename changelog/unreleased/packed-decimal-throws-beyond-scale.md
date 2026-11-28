type: breaking

Binary loaders now throw (a `FieldConversionException` wrapping an `OverflowException`) when a `PackedDecimal` value has more decimal places than the field's `Scale`, instead of silently truncating the extra digits; round the value first if truncation was intended.
