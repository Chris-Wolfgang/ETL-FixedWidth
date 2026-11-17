type: fix

`PackedDecimal` (COMP-3) decoding now rejects a sign nibble below 0xA with `FormatException`, and encoding no longer writes negative zero (`0x0D`) for a value that truncates to zero.
