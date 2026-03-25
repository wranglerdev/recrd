using System.Text.Json.Nodes;

namespace Recrd.Core.Ast;

public sealed record RecordedEvent(
    Guid Id,
    long TimestampMs,
    string EventType,
    Selector[] Selectors,
    JsonNode? Payload,
    string? DataVariable
);
