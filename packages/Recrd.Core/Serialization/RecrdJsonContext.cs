using System.Text.Json;
using System.Text.Json.Serialization;
using Recrd.Core.Ast;
using Recrd.Core.Interfaces;
using Recrd.Core.Pipeline;

namespace Recrd.Core.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Metadata,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Session))]
[JsonSerializable(typeof(SessionMetadata))]
[JsonSerializable(typeof(ViewportSize))]
[JsonSerializable(typeof(Variable))]
[JsonSerializable(typeof(IStep))]
[JsonSerializable(typeof(ActionStep))]
[JsonSerializable(typeof(AssertionStep))]
[JsonSerializable(typeof(GroupStep))]
[JsonSerializable(typeof(ActionType))]
[JsonSerializable(typeof(AssertionType))]
[JsonSerializable(typeof(GroupType))]
[JsonSerializable(typeof(SelectorStrategy))]
[JsonSerializable(typeof(Selector))]
[JsonSerializable(typeof(RecordedEvent))]
[JsonSerializable(typeof(RecordedEventType))]
[JsonSerializable(typeof(CompilerOptions))]
[JsonSerializable(typeof(CompilationResult))]
[JsonSerializable(typeof(IReadOnlyList<IStep>))]
[JsonSerializable(typeof(IReadOnlyList<Variable>))]
[JsonSerializable(typeof(IReadOnlyList<Selector>))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(IReadOnlyList<SelectorStrategy>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, string>))]
[JsonSerializable(typeof(IReadOnlyDictionary<SelectorStrategy, string>))]
public sealed partial class RecrdJsonContext : JsonSerializerContext { }
