using Recrd.Core.Ast;

namespace Recrd.Core.Pipeline;

public sealed record RecordedEvent
{
    public string Id { get; }
    public long TimestampMs { get; }
    public RecordedEventType EventType { get; }
    public IReadOnlyList<Selector> Selectors { get; }
    public IReadOnlyDictionary<string, string> Payload { get; }
    public string? DataVariable { get; init; }

    public RecordedEvent(
        string id,
        long timestampMs,
        RecordedEventType eventType,
        IReadOnlyList<Selector> selectors,
        IReadOnlyDictionary<string, string> payload,
        string? dataVariable = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TimestampMs = timestampMs;
        EventType = eventType;
        Selectors = selectors ?? throw new ArgumentNullException(nameof(selectors));
        Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        DataVariable = dataVariable;
    }
}
