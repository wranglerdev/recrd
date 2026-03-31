using Xunit;

namespace Recrd.Recording.Tests;

public class EventCaptureTests
{
    [Fact]
    public async Task Capture_ClickEvent_HasCorrectEventType()
        => Assert.Fail("Red: REC-02 — Click event capture not implemented");

    [Fact]
    public async Task Capture_InputChangeEvent_HasCorrectEventType()
        => Assert.Fail("Red: REC-03 — InputChange event capture not implemented");

    [Fact]
    public async Task Capture_SelectEvent_HasCorrectEventType()
        => Assert.Fail("Red: REC-03 — Select event capture not implemented");

    [Fact]
    public async Task Capture_HoverEvent_HasCorrectEventType()
        => Assert.Fail("Red: REC-03 — Hover event capture not implemented");

    [Fact]
    public async Task Capture_NavigationEvent_HasCorrectEventType()
        => Assert.Fail("Red: REC-02 — Navigation event capture not implemented");

    [Fact]
    public async Task Capture_FileUploadEvent_HasCorrectEventType()
        => Assert.Fail("Red: REC-03 — FileUpload event capture not implemented");

    [Fact]
    public async Task Capture_DragDropEvent_HasCorrectEventType()
        => Assert.Fail("Red: REC-03 — DragDrop event capture not implemented");

    [Fact]
    public async Task JsAgent_InjectedOnNavigation()
        => Assert.Fail("Red: REC-02 — JS agent injection on navigation not implemented");

    [Fact]
    public async Task SelectorExtraction_ProducesAtLeastThreeStrategies()
        => Assert.Fail("Red: REC-05 — selector extraction producing ≥3 strategies not implemented");

    [Fact]
    public async Task SelectorExtraction_RankingOrder_DataTestIdFirst()
        => Assert.Fail("Red: REC-05 — selector ranking order DataTestId > Id > Role > Css > XPath not implemented");

    [Fact]
    public async Task CapturedEvent_PushedToChannel()
        => Assert.Fail("Red: REC-04 — event pushed to IRecordingChannel not implemented");
}
