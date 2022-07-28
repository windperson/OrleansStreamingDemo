using Moq;
using Orleans.Providers;
using Orleans.Streams;
using Orleans.TestingHost;
using Orleans.Timers;
using OrleansStreamingDemo.Grains.Interfaces;

namespace OrleansStreamingDemo.Grains.Tests;

public class ClientReceiveStreamingTest
{
    private const string StreamProviderName = "DemoOrleansStreamProvider";

    private class TestSiloAndClientConfigurator : ISiloConfigurator, IClientBuilderConfigurator
    {
        public static Func<object, Task>? TimerTick { get; private set; }

        public void Configure(ISiloBuilder siloBuilder)
        {
            var mockTimerRegistry = new Mock<ITimerRegistry>();
            mockTimerRegistry.Setup(x =>
                    x.RegisterTimer(It.IsAny<Grain>(),
                        It.IsAny<Func<object, Task>>(), It.IsAny<object>(), It.IsAny<TimeSpan>(), It.IsAny<TimeSpan>()))
                .Returns(new Mock<IDisposable>().Object)
                .Callback((Grain targetGrain, Func<object, Task>? timerTick, object _, TimeSpan _, TimeSpan _) =>
                {
                    if (targetGrain is ProducerGrain && timerTick != null)
                    {
                        TimerTick = timerTick;
                    }
                });
            siloBuilder.AddMemoryGrainStorage("PubSubStore")
                .AddMemoryStreams<DefaultMemoryMessageBodySerializer>(StreamProviderName)
                .ConfigureServices(services => { services.AddSingleton(mockTimerRegistry.Object); });
        }

        public void Configure(IConfiguration configuration, IClientBuilder clientBuilder)
        {
            clientBuilder.AddMemoryStreams<DefaultMemoryMessageBodySerializer>(StreamProviderName);
        }
    }


    [Fact]
    public async Task TestGrainStreamingToOtherGrain()
    {
        // Arrange
        var count = -1;

        Task OnNextAsync(int item, StreamSequenceToken? token = null)
        {
            count = item;
            return Task.CompletedTask;
        }

        var key = Guid.NewGuid();
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloAndClientConfigurator>();
        builder.AddClientBuilderConfigurator<TestSiloAndClientConfigurator>();
        var cluster = builder.Build();
        await cluster.DeployAsync();

        // Act
        var streamProvider = cluster.Client.GetStreamProvider(StreamProviderName);
        var stream = streamProvider.GetStream<int>(key, "demo-streaming-namespace");
        await stream.SubscribeAsync(OnNextAsync);

        var producerGrain = cluster.Client.GetGrain<IProducerGrain>("Demo streaming");
        await producerGrain.StartProducing("demo-streaming-namespace", key);
        await TestSiloAndClientConfigurator.TimerTick?.Invoke(new object())!;
        await TestSiloAndClientConfigurator.TimerTick?.Invoke(new object())!;
        await TestSiloAndClientConfigurator.TimerTick?.Invoke(new object())!;
        //Give some time for stream to propagate data to target grain 
        await Task.Delay(TimeSpan.FromSeconds(0.3));
        await producerGrain.StopProducing();
        await cluster.StopAllSilosAsync();

        // Assert
        Assert.True(count == 2);
    }
}