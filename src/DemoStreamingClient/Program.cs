using System;
using System.Threading.Tasks;
using Orleans;
using Orleans.Hosting;
using Orleans.Streams;
using OrleansStreamingDemo.Grains.Interfaces;
using Serilog;

namespace DemoStreamingClient
{
    class Program
    {
        private const string DefaultAzuriteQueueCon =
            @"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            
            Log.Logger.Information("\r\n=== Starting Orleans Streaming Receiving Demo ===\r\n");

            var clientBuilder = new ClientBuilder();
            clientBuilder.UseLocalhostClustering()
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IProducerGrain).Assembly).WithReferences())
                .AddAzureQueueStreams("DemoOrleansStreamProvider",
                    optionsBuilder =>
                        optionsBuilder.Configure(options => options.ConfigureQueueServiceClient(DefaultAzuriteQueueCon)))
                .ConfigureLogging(logging => logging.AddSerilog());

            var client = clientBuilder.Build();

            Log.Logger.Information("Press any key to start connecting to Silo");
            Console.ReadKey();

            await client.Connect();
            Log.Logger.Information("\r\nConnected to Silo, press any key to start receive streaming demo\r\n");
            Console.ReadKey();

            var streamProvider = client.GetStreamProvider("DemoOrleansStreamProvider");
            var key = Guid.NewGuid();
            var stream = streamProvider.GetStream<int>(key, "demo-streaming-namespace");
            await stream.SubscribeAsync(OnNextAsync);

            var producerGrain = client.GetGrain<IProducerGrain>("Demo streaming");
            await producerGrain.StartProducing("demo-streaming-namespace", key);

            Log.Logger.Information("\r\nPress any key to stop\r\n");
            Console.ReadKey();

            await producerGrain.StopProducing();
            await client.Close();
        }

        static Task OnNextAsync(int item, StreamSequenceToken? token = null)
        {
            Log.Logger.Information("OnNextAsync: item: {0}, token = {1}", item, token);
            return Task.CompletedTask;
        }
    }
}