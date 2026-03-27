# Phase 4: Gherkin Generator - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions captured in CONTEXT.md — this log preserves the Q&A.

**Date:** 2026-03-27
**Phase:** 04-gherkin-generator
**Mode:** discuss
**Areas discussed:** Step text templates, Feature file structure, GherkinException design, Generator API shape

---

## Areas Discussed

### Step text templates

| Question | Options presented | Selected |
|----------|------------------|----------|
| How should steps reference the target element? | Use best selector value; Full selector expression; Omit selector | Use best selector value |

**Detail:** `data-testid` → `id` → `role` preferred. Selector value quoted in step text. Variable placeholders use `<variable_name>` syntax.

**Move to next area after:** 1 question — area clear.

---

### Feature file structure

| Question | Options presented | Selected |
|----------|------------------|----------|
| What should appear as the Feature: name? | Session base URL; Session ID (UUID); Fixed placeholder | Session base URL |
| Should the scenario include tags? | No tags; @generated tag; Configurable via CompilerOptions | Configurable via CompilerOptions |

**Move to next area after:** 2 questions — area clear.

---

### GherkinException design

| Question | Options presented | Selected |
|----------|------------------|----------|
| What structured properties should GherkinException carry? | VariableName + DataFilePath; Enriched: + session ID + step index | VariableName + DataFilePath |

**Move to next area after:** 1 question — area clear.

---

### Generator API shape

| Question | Options presented | Selected |
|----------|------------------|----------|
| How should Recrd.Gherkin expose its generator? | Interface + class; Static class / extension methods | Interface + class |

**Move to next area after:** 1 question — area clear.

---

## Corrections Made

No corrections — all selections confirmed on first pass.

---

## External Research

None performed — codebase and REQUIREMENTS.md provided sufficient context.
