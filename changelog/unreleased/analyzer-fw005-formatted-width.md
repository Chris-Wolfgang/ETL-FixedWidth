type: fix

Analyzer FW005 now compares the minimum written width of a `Format` pattern with the field length instead of the pattern length, so quoted literals and escapes no longer cause false positives and standard single-character specifiers are no longer misjudged.
