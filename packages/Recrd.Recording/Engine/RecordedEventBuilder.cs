using System.Text.Json;
using Recrd.Core.Ast;
using Recrd.Core.Pipeline;
using Recrd.Recording.Selectors;

namespace Recrd.Recording.Engine;

/// <summary>
/// Builds <see cref="RecordedEvent"/> instances from the JSON payload received via
/// the <c>window.__recrdCapture</c> ExposeFunctionAsync callback.
/// </summary>
internal static class RecordedEventBuilder
{
    private static readonly IReadOnlyDictionary<string, RecordedEventType> _typeMap =
        new Dictionary<string, RecordedEventType>(StringComparer.Ordinal)
        {
            ["Click"] = RecordedEventType.Click,
            ["InputChange"] = RecordedEventType.InputChange,
            ["Select"] = RecordedEventType.Select,
            ["Hover"] = RecordedEventType.Hover,
            ["Navigation"] = RecordedEventType.Navigation,
            ["FileUpload"] = RecordedEventType.FileUpload,
            ["DragDrop"] = RecordedEventType.DragDrop,
        };

    private static readonly HashSet<string> _specialTypes = new(StringComparer.Ordinal)
    {
        "TagStart", "TagConfirm", "AssertStart", "AssertConfirm"
    };

    /// <summary>
    /// Parses the JSON payload and returns a <see cref="RecordedEvent"/> for regular events,
    /// or <c>null</c> for special event types (TagStart, TagConfirm, AssertStart, AssertConfirm).
    /// </summary>
    public static RecordedEvent? Build(string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
            return null;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonPayload);
        }
        catch (JsonException)
        {
            return null;
        }

        using (doc)
        {
            var root = doc.RootElement;

            var typeStr = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;
            if (typeStr is null)
                return null;

            // Special types are handled by ParseSpecialEvent — skip here
            if (_specialTypes.Contains(typeStr))
                return null;

            if (!_typeMap.TryGetValue(typeStr, out var eventType))
                return null;

            var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (string.IsNullOrEmpty(id))
                id = "evt-unknown";

            var timestamp = root.TryGetProperty("timestamp", out var tsProp)
                ? (long)tsProp.GetDouble()
                : 0L;

            // Parse selectors
            var selectors = new List<Selector>();
            if (root.TryGetProperty("selectors", out var selectorsEl)
                && selectorsEl.ValueKind == JsonValueKind.Object)
            {
                selectors.Add(SelectorExtractor.Extract(selectorsEl));
            }

            // Parse payload dictionary
            var payload = new Dictionary<string, string>();
            if (root.TryGetProperty("payload", out var payloadEl)
                && payloadEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in payloadEl.EnumerateObject())
                {
                    var val = prop.Value.ValueKind == JsonValueKind.String
                        ? prop.Value.GetString() ?? string.Empty
                        : prop.Value.ToString();
                    payload[prop.Name] = val;
                }
            }

            // Popup scope marker — added when event originates from a page opened via window.open()
            var isPopup = root.TryGetProperty("isPopup", out var isPopupProp)
                && isPopupProp.ValueKind == JsonValueKind.True;
            if (isPopup)
                payload["__popupScope"] = "true";

            return new RecordedEvent(
                Id: id,
                TimestampMs: timestamp,
                EventType: eventType,
                Selectors: selectors.AsReadOnly(),
                Payload: payload);
        }
    }

    /// <summary>
    /// Returns the type string and full JSON data for special event types
    /// (TagStart, TagConfirm, AssertStart, AssertConfirm), or <c>null</c> for regular events.
    /// </summary>
    public static (string Type, JsonElement Data)? ParseSpecialEvent(string jsonPayload)
    {
        if (string.IsNullOrWhiteSpace(jsonPayload))
            return null;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonPayload);
        }
        catch (JsonException)
        {
            return null;
        }

        // Note: doc is intentionally not disposed here — caller owns the returned JsonElement
        var root = doc.RootElement;
        var typeStr = root.TryGetProperty("type", out var typeProp) ? typeProp.GetString() : null;

        if (typeStr is not null && _specialTypes.Contains(typeStr))
        {
            return (typeStr, root.Clone());
        }

        doc.Dispose();
        return null;
    }
}
