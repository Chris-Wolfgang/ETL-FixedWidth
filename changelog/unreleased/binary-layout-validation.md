type: fix

Binary stages now validate the record layout when the map is built - binary integer length (1-8 bytes), non-negative `Scale`, and public getter/setter - with errors that name the type and property, and `FixedWidthBinaryLoader` no longer requires a parameterless constructor on the record type.
