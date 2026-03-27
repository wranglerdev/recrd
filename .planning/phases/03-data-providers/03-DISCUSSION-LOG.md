# Phase 3: Data Providers - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-26
**Phase:** 03-data-providers
**Areas discussed:** CSV parsing approach, JSON nested array handling, DataParseException richness

---

## CSV Parsing Approach

| Option | Description | Selected |
|--------|-------------|----------|
| Use CsvHelper | JoshClose/CsvHelper NuGet — RFC 4180 compliant, BOM-tolerant, configurable delimiter, actively maintained. Adds one NuGet dep to Recrd.Data. | ✓ |
| Custom RFC 4180 parser | Write in-house parser. Zero new dependencies, full control, but requires implementing quoted-field handling, escaped quotes, BOM stripping, multiline values. | |
| Use TextFieldParser (BCL) | Microsoft.VisualBasic.FileIO legacy API — not RFC 4180 strict, no good streaming support. | |

**User's choice:** CsvHelper
**Notes:** None

---

## JSON Nested Array Handling

| Option | Description | Selected |
|--------|-------------|----------|
| Skip array fields silently | Array fields (e.g., `tags:["a","b"]`) are ignored and not included in the output dictionary. | ✓ |
| Index-flatten to dot-notation | `tags:["a","b"]` becomes `tags.0` and `tags.1` as separate columns. | |
| Throw DataParseException on array fields | Any array field triggers a parse error. | |

**User's choice:** Skip silently
**Notes:** None

---

## DataParseException Richness

| Option | Description | Selected |
|--------|-------------|----------|
| Enriched: line + raw line + file path | `LineNumber`, `OffendingLine` (raw text), `FilePath` — maximum diagnostic info for CI debugging. | ✓ |
| Minimal: line number only | Just `LineNumber` as specified in requirements. | |
| Enriched + column info | `LineNumber`, `OffendingLine`, `FilePath`, `ColumnName`/`ColumnIndex`. | |

**User's choice:** Enriched (LineNumber + OffendingLine + FilePath)
**Notes:** None

---

## Claude's Discretion

- CsvHelper version to pin
- `CsvDataProvider` streaming adapter shape
- Null/empty cell representation in output dictionary
- Delimiter configuration pattern (builder vs constructor)

## Deferred Ideas

None
