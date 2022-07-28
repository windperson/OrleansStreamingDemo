using Microsoft.Extensions.Logging;
using Moq;
using Orleans.Providers;
using Orleans.Streams;
using Orleans.TestingHost;
using OrleansStreamingDemo.Grains.Interfaces;
using Orleans.Timers;

namespace OrleansStreamingDemo.Grains.Tests;

public class ProducerConsumerGrainTest
{
    private static Mock<ILogger<ConsumerGrain>>? _mockLogger;


    private class SiloBuilder : ISiloConfigurator
    {
        public static Func<object, Task>? TimerTick { get; private set; }
        public void Configure(ISiloBuilder siloBuilder)
        {
            _mockLogger = new Mock<ILogger<ConsumerGrain>>();
            var mockLoggerFactory = new Mock<ILoggerFactory>();
            mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() => _mockLogger.Object);

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

            siloBuilder
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryStreams<DefaultMemoryMessageBodySerializer>("DemoOrleansStreamProvider")
                .ConfigureServices(services =>
                {
                    services.AddSingleton(mockLoggerFactory.Object);
                    services.AddSingleton(mockTimerRegistry.Object);
                });
        }
    }

    [Fact]
    public async Task TestGrainStreamingToOtherGrain()
    {
        // Arrange
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<SiloBuilder>();
        var cluster = builder.Build();
        await cluster.DeployAsync();

        //Act
        var key = Guid.NewGuid();
        var producerGrain = cluster.GrainFactory.GetGrain<IProducerGrain>("Demo streaming");

        await producerGrain.StartProducing("demo-streaming-namespace", key);
        await SiloBuilder.TimerTick?.Invoke(new object())!;
        await SiloBuilder.TimerTick?.Invoke(new object())!;
        //Give some time for stream to propagate data to target receiver
        await Task.Delay(TimeSpan.FromSeconds(0.3));
        await producerGrain.StopProducing();
        await cluster.StopAllSilosAsync();

        //Assert
        Assert.NotNull(_mockLogger);

        _mockLogger!.VerifyLog( logger =>
                // ReSharper disable once ComplexObjectDestructuringProblem
                logger.LogInformation("OnNextAsync: item: {Item}, token = {Token}", 
                    It.IsAny<int>(), It.IsAny<StreamSequenceToken>()),
            Times.Exactly(2));
    }
}