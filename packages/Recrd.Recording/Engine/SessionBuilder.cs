using Recrd.Core.Ast;
using Recrd.Core.Pipeline;

namespace Recrd.Recording.Engine;

/// <summary>
/// Builds an in-memory <see cref="Session"/> from a stream of <see cref="RecordedEvent"/> instances
/// and manually added steps/variables.
/// </summary>
internal class SessionBuilder
{
    private readonly List<IStep> _steps = new();
    private readonly List<Variable> _variables = new();
    private readonly SessionMetadata _metadata;

    public SessionBuilder(SessionMetadata metadata)
    {
        _metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
    }

    public void AddStep(IStep step) => _steps.Add(step);

    public void AddVariable(Variable variable) => _variables.Add(variable);

    public bool HasVariable(string name) =>
        _variables.Any(v => string.Equals(v.Name, name, StringComparison.Ordinal));

    /// <summary>
    /// Converts a <see cref="RecordedEvent"/> to the appropriate <see cref="IStep"/>.
    /// Uses the first (highest-priority) selector for the ActionStep.
    /// </summary>
    public IStep ConvertToStep(RecordedEvent evt)
    {
        ArgumentNullException.ThrowIfNull(evt);

        // Provide a fallback selector if the event has no selectors (e.g. Navigation)
        var selector = evt.Selectors.Count > 0
            ? evt.Selectors[0]
            : new Selector(
                new List<SelectorStrategy>().AsReadOnly(),
                new Dictionary<SelectorStrategy, string>());

        var actionType = evt.EventType switch
        {
            RecordedEventType.Click => ActionType.Click,
            RecordedEventType.InputChange => ActionType.Type,
            RecordedEventType.Select => ActionType.Select,
            RecordedEventType.Hover => ActionType.Click,   // hover recorded as click action
            RecordedEventType.Navigation => ActionType.Navigate,
            RecordedEventType.FileUpload => ActionType.Upload,
            RecordedEventType.DragDrop => ActionType.DragDrop,
            _ => ActionType.Click,
        };

        return new ActionStep(actionType, selector, evt.Payload);
    }

    /// <summary>
    /// Builds and returns the current <see cref="Session"/> snapshot.
    /// Can be called multiple times (for partial snapshots).
    /// </summary>
    public Session Build() => new Session(
        SchemaVersion: 1,
        Metadata: _metadata,
        Variables: _variables.AsReadOnly(),
        Steps: _steps.AsReadOnly());
}
