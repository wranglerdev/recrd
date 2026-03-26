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
        string Id,
        long TimestampMs,
        RecordedEventType EventType,
        IReadOnlyList<Selector> Selectors,
        IReadOnlyDictionary<string, string> Payload,
        string? DataVariable = null)
    {
        this.Id = Id ?? throw new ArgumentNullException(nameof(Id));
        this.TimestampMs = TimestampMs;
        this.EventType = EventType;
        this.Selectors = Selectors ?? throw new ArgumentNullException(nameof(Selectors));
        this.Payload = Payload ?? throw new ArgumentNullException(nameof(Payload));
        this.DataVariable = DataVariable;
    }
}
