using Serilog;
using System;
using System.Threading.Tasks;
using Orleans;
using OrleansStreamingDemo.Grains.Interfaces;

namespace DemoClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            
            Log.Logger.Information("\r\n=== Starting Orleans Streaming Demo ===\r\n");
            
            var clientBuilder = new ClientBuilder();
            clientBuilder.UseLocalhostClustering()
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IProducerGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddSerilog());
            
            var client = clientBuilder.Build();
            
            Log.Logger.Information("Press any key to start connecting to Silo");
            Console.ReadKey();
            
            await client.Connect();
            Log.Logger.Information("\r\nConnected to Silo, press any key to start streaming demo in ProducerGrain\r\n");
            Console.ReadKey();
            
            var key = Guid.NewGuid();
            var producerGrain = client.GetGrain<IProducerGrain>("Demo streaming");
            await producerGrain.StartProducing("demo-streaming-namespace", key);
            
            Log.Logger.Information("\r\nPress any key to stop streaming demo in ProducerGrain\r\n");
            Console.ReadKey();
            await producerGrain.StopProducing();
            Log.Logger.Information("Stopped streaming demo in ProducerGrain, press any key to disconnect from Silo");
            Console.ReadKey();
            await client.Close();
        }
    }
}
