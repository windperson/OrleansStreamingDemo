using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using Orleans.Streams.Core;
using OrleansStreamingDemo.Grains.Interfaces;

namespace OrleansStreamingDemo.Grains;

[ImplicitStreamSubscription("demo-streaming-namespace")]
public class ConsumerGrain : Grain , IConsumerGrain, IStreamSubscriptionObserver
{
    private readonly ILogger<ConsumerGrain> _logger;
    private readonly LoggerObserver _observer;

    public ConsumerGrain(ILogger<ConsumerGrain> logger)
    {
        _logger = logger;
        _observer = new LoggerObserver(_logger);
    }
    
    // Called when a subscription is added
    public async Task OnSubscribed(IStreamSubscriptionHandleFactory handleFactory)
    {
        var handle = handleFactory.Create<int>();
        await handle.ResumeAsync(_observer);
    }
}

internal class LoggerObserver : IAsyncObserver<int>
{
    private readonly ILogger<ConsumerGrain> _logger;

    // ReSharper disable once ContextualLoggerProblem
    public LoggerObserver(ILogger<ConsumerGrain> logger)
    {
        _logger = logger;
    }

    public Task OnNextAsync(int item, StreamSequenceToken? token = default)
    {
        // ReSharper disable once ComplexObjectDestructuringProblem
        _logger.LogInformation("OnNextAsync: item: {Item}, token = {Token}", item, token);
        return Task.CompletedTask;
    }

    public Task OnCompletedAsync()
    {
        _logger.LogInformation("OnCompletedAsync");
        return Task.CompletedTask;
    }

    public Task OnErrorAsync(Exception ex)
    {
        _logger.LogInformation("OnErrorAsync: {Exception}", ex);
        return Task.CompletedTask;
    }
}