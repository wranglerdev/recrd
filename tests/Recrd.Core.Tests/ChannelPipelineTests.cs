using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Recrd.Core.Ast;
using Recrd.Core.Pipeline;
using Xunit;

namespace Recrd.Core.Tests;

public sealed class ChannelPipelineTests
{
    private static RecordedEvent MakeEvent(int index = 0) =>
        new RecordedEvent(
            Id: Guid.NewGuid().ToString(),
            TimestampMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + index,
            EventType: RecordedEventType.Click,
            Selectors: new List<Selector>
            {
                new Selector(
                    Strategies: new List<SelectorStrategy> { SelectorStrategy.DataTestId },
                    Values: new Dictionary<SelectorStrategy, string>
                    {
                        [SelectorStrategy.DataTestId] = $"[data-testid=\"item-{index}\"]"
                    }
                )
            },
            Payload: new Dictionary<string, string>(),
            DataVariable: null
        );

    [Fact]
    public async Task RecordingChannel_WriteAndRead_SingleEvent()
    {
        var channel = new RecordingChannel(capacity: 2);
        var evt = MakeEvent();

        await channel.WriteAsync(evt);
        channel.Complete();

        var results = new List<RecordedEvent>();
        await foreach (var item in channel.ReadAllAsync())
        {
            results.Add(item);
        }

        Assert.Single(results);
        Assert.Equal(evt.Id, results[0].Id);
    }

    [Fact]
    public async Task RecordingChannel_Backpressure_BlocksWhenFull()
    {
        var channel = new RecordingChannel(capacity: 1);
        var firstEvent = MakeEvent(0);
        var secondEvent = MakeEvent(1);

        // Fill the channel
        await channel.WriteAsync(firstEvent);

        // Second write should block because channel is full
        var writeTask = channel.WriteAsync(secondEvent).AsTask();
        var completedTask = await Task.WhenAny(writeTask, Task.Delay(100));

        // The write should still be pending (blocked by backpressure)
        Assert.NotSame(writeTask, completedTask);

        // Unblock by reading
        var readResults = new List<RecordedEvent>();
        await foreach (var item in channel.ReadAllAsync())
        {
            readResults.Add(item);
            if (readResults.Count == 1) break; // read one to unblock the writer
        }

        // Now the second write should be able to complete
        await writeTask;
        channel.Complete();
    }

    [Fact]
    public async Task RecordingChannel_Cancellation_StopsRead()
    {
        var channel = new RecordingChannel(capacity: 10);
        using var cts = new CancellationTokenSource();

        var readTask = Task.Run(async () =>
        {
            var results = new List<RecordedEvent>();
            await foreach (var item in channel.ReadAllAsync(cts.Token))
            {
                results.Add(item);
            }
            return results;
        });

        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<OperationCanceledException>(() => readTask);
    }

    [Fact]
    public async Task RecordingChannel_DrainWithoutDeadlock()
    {
        var channel = new RecordingChannel(capacity: 10);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        for (int i = 0; i < 5; i++)
        {
            await channel.WriteAsync(MakeEvent(i));
        }
        channel.Complete();

        var results = new List<RecordedEvent>();
        await foreach (var item in channel.ReadAllAsync(cts.Token))
        {
            results.Add(item);
        }

        Assert.Equal(5, results.Count);
    }

    [Fact]
    public void RecordedEvent_HasAllRequiredFields()
    {
        var id = Guid.NewGuid().ToString();
        var timestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var eventType = RecordedEventType.Click;
        var selectors = new List<Selector>();
        var payload = new Dictionary<string, string> { ["key"] = "value" };
        string? dataVariable = "my_var";

        var evt = new RecordedEvent(
            Id: id,
            TimestampMs: timestampMs,
            EventType: eventType,
            Selectors: selectors,
            Payload: payload,
            DataVariable: dataVariable
        );

        Assert.Equal(id, evt.Id);
        Assert.Equal(timestampMs, evt.TimestampMs);
        Assert.Equal(eventType, evt.EventType);
        Assert.Same(selectors, evt.Selectors);
        Assert.Same(payload, evt.Payload);
        Assert.Equal("my_var", evt.DataVariable);
    }
}
