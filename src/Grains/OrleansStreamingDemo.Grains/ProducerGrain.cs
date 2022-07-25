using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Streams;
using OrleansStreamingDemo.Grains.Interfaces;

namespace OrleansStreamingDemo.Grains;

public class ProducerGrain : Grain, IProducerGrain
{
    private readonly ILogger<IProducerGrain> _logger;

    private IAsyncStream<int>? _stream;
    private IDisposable? _timer;

    private int _counter = 0;
    
    public ProducerGrain(ILogger<ProducerGrain> logger )
    {
        _logger = logger;
    }
    
    public Task StartProducing(string streamNameSpace, Guid key)
    {
        if (_timer is not null)
        {
            throw new Exception("This grain is already producing events");
        }
        
        // Get the stream
        _stream = GetStreamProvider("DemoOrleansStreamProvider").GetStream<int>(key, streamNameSpace);
        
        //Register a timer that produce an event every second
        var period = TimeSpan.FromSeconds(1);
        _timer = RegisterTimer(TimerTick, null, period, period);
        
        _logger.LogInformation("I will produce a new event every {Period}", period);
        return Task.CompletedTask;
    }

    private async Task TimerTick(object _)
    {
        var value = _counter++;
        _logger.LogInformation("Producing event {EventNumber}", value);
        if(_stream is not null)
        {
            await _stream.OnNextAsync(value);
        }
    }

    public Task StopProducing()
    {
        if(_timer is not null)
        {
            _timer.Dispose();
            _timer = null;
        }
        if(_stream is not null)
        {
            _stream = null;
        }
        return Task.CompletedTask;
    }
}