# Phase 2: Core AST Types & Interfaces - Discussion Log

> **Audit trail only.** Do not use as input to planning, research, or execution agents.
> Decisions are captured in CONTEXT.md — this log preserves the alternatives considered.

**Date:** 2026-03-26
**Phase:** 02 - Core AST Types & Interfaces
**Areas discussed:** C# type modeling, JSON serialization strategy, Channel\<T\> design, Test structure

---

## C# Type Modeling

| Option | Description | Selected |
|--------|-------------|----------|
| C# records (sealed, immutable) | Init-only properties, structural equality, with-expressions, clean pattern matching | ✓ |
| Abstract base class + sealed subclasses | OOP hierarchy, virtual methods support | |
| Classes with init-only setters | Flexible, less ceremony | |

**User's choice:** Sealed records

---

### ActionStep subtype modeling

| Option | Description | Selected |
|--------|-------------|----------|
| Single ActionStep with ActionType enum | One record, enum discriminator, Payload dict | ✓ |
| Separate sealed records per action | ClickStep, TypeStep, etc. — strongest typing | |
| Discriminated union via OneOf | Third-party library, max type safety | |

**User's choice:** Single ActionStep record with ActionType enum

---

### AssertionStep subtype modeling

| Option | Description | Selected |
|--------|-------------|----------|
| Single AssertionStep with AssertionType enum | One record, enum discriminator, Payload dict | ✓ |
| Separate sealed records per assertion type | TextEqualsStep, VisibleStep, etc. | |

**User's choice:** Single AssertionStep record with AssertionType enum

---

## JSON Serialization Strategy

### Library choice

| Option | Description | Selected |
|--------|-------------|----------|
| System.Text.Json | Built-in, source-gen, no NuGet dep | ✓ |
| Newtonsoft.Json | More features, easier converters, but adds dep | |

**User's choice:** System.Text.Json

---

### Polymorphic serialization approach

| Option | Description | Selected |
|--------|-------------|----------|
| [JsonPolymorphic] + [JsonDerivedType] | Built-in .NET 7+ attribute-driven, $type discriminator | ✓ |
| Custom JsonConverter\<IStep\> | Hand-written, full control | |
| Wrapper with nullable fields | Flat StepDto, no polymorphism | |

**User's choice:** [JsonPolymorphic] built-in polymorphism

---

### Source generation

| Option | Description | Selected |
|--------|-------------|----------|
| Yes — source-generated context | AOT-compatible, faster startup, required for single-file publish | ✓ |
| No — runtime reflection | Simpler setup | |

**User's choice:** Yes, source-generated JsonSerializerContext

---

## Channel\<T\> Design

### Wrapper vs direct

| Option | Description | Selected |
|--------|-------------|----------|
| Thin wrapper class (RecordingChannel / EventPipeline) | Mockable interface, explicit surface, testable | ✓ |
| Expose System.Threading.Channels.Channel\<T\> directly | Zero abstraction, tight BCL coupling | |

**User's choice:** Thin wrapper with interface

---

### Capacity

| Option | Description | Selected |
|--------|-------------|----------|
| Bounded with configurable capacity (default 1000) | Backpressure, memory safety, injectable for tests | ✓ |
| Unbounded | Never blocks WriteAsync, simpler | |

**User's choice:** Bounded, configurable capacity

---

## Test Structure

### Organization

| Option | Description | Selected |
|--------|-------------|----------|
| Grouped by behavior suite | 5 files covering behavior areas | ✓ |
| One test class per type | Mirrors type structure | |

**User's choice:** Behavior-suite organization (SessionSerializationTests, StepModelTests, SelectorVariableTests, ChannelPipelineTests, InterfaceContractTests)

---

### Subtype parametrization

| Option | Description | Selected |
|--------|-------------|----------|
| Theory + [MemberData] | One method covers all enum values | ✓ |
| Individual [Fact] per subtype | Verbose but pinpoints failures | |

**User's choice:** Theory + [MemberData]

---

### TDD red-phase commit strategy

| Option | Description | Selected |
|--------|-------------|----------|
| All test files in one red commit | One atomic red commit on tdd/phase-02 branch | ✓ |
| Interleaved per-file | Red then green file by file | |

**User's choice:** All test files committed red atomically

---

## Claude's Discretion

- Channel wrapper class and interface names
- JSON property naming convention
- Selector priority array representation
- Variable validation placement (constructor vs factory)
- Payload dictionary value types
