# Introduction

Welcome to Wolfgang.Etl.FixedWidth!

## Overview

Wolfgang.Etl.FixedWidth provides an extractor and loader for reading and writing fixed-width text files, built on [Wolfgang.Etl.Abstractions](https://www.nuget.org/packages/Wolfgang.Etl.Abstractions). It maps fixed-width columns to strongly typed POCO properties using declarative attributes, handles alignment, padding, parsing, and formatting, and streams records asynchronously with `IAsyncEnumerable<T>`.

## Key Features

- **Attribute-based field mapping** — `[FixedWidthField]` and `[FixedWidthSkip]` attributes define column index, length, alignment, padding, and header text
- **Left/right alignment with configurable padding** — control how values are positioned within their column and which pad character is used
- **Custom ValueParser and ValueConverter delegates** — plug in custom parsing (extraction) and conversion (loading) logic per field or globally
- **Header rows, separator lines, field delimiters** — optional header output, configurable separator characters, and inter-field delimiters
- **BlankLineHandling and MalformedLineHandling modes** — choose to skip, error, or pass through blank and malformed lines during extraction
- **SkipItemCount / MaximumItemCount pagination** — skip the first N records and cap the total extracted or loaded
- **LineFilter for custom line control** — supply a delegate to include or exclude lines before parsing
- **Timer-based progress reporting** — periodic `FixedWidthReport` snapshots via the Abstractions progress-timer infrastructure
- **Zero-copy parsing with ReadOnlyMemory&lt;char&gt;** — slices line data without allocating intermediate strings where possible
- **Span-based numeric parsing on net8.0+** — uses `ISpanParsable<T>` for allocation-free numeric conversion on modern runtimes
- **Compiled delegates for reflection-free field access** — property getters and setters are compiled once and cached for fast field access
- **Multi-TFM support** — targets netstandard2.0, netstandard2.1, net8.0, net9.0, and net10.0

## Getting Help

If you need help with Wolfgang.Etl.FixedWidth, please:

- Check the [Getting Started](getting-started.md) guide
- Review the [API Reference](../api/index.md)
- Visit the [GitHub repository](https://github.com/Chris-Wolfgang/ETL-FixedWidth)
- Open an issue on [GitHub Issues](https://github.com/Chris-Wolfgang/ETL-FixedWidth/issues)
