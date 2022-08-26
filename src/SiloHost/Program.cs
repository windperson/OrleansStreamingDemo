using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Hosting;
using Serilog;

namespace SiloHost
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateLogger();

            var hostBuilder = new HostBuilder();

            hostBuilder.UseConsoleLifetime()
                .UseSerilog()
                .UseOrleans(ConfigureSilo);

            var host = hostBuilder.Build();

            await host.RunAsync();
        }

        private const string DefaultAzuriteTableCon =
            @"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

        private const string DefaultAzuriteQueueCon =
            @"DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;QueueEndpoint=http://127.0.0.1:10001/devstoreaccount1;TableEndpoint=http://127.0.0.1:10002/devstoreaccount1;";

        private static void ConfigureSilo(ISiloBuilder siloBuilder)
        {
            siloBuilder.UseLocalhostClustering()
                .AddAzureTableGrainStorage("PubSubStore",
                    optionBuilder => optionBuilder.ConfigureTableServiceClient(DefaultAzuriteTableCon))
                .AddAzureQueueStreams("DemoOrleansStreamProvider", ConfigureAzureQueue);
        }

        private static void ConfigureAzureQueue(OptionsBuilder<AzureQueueOptions> optionsBuilder)
        {
            optionsBuilder.Configure(options => { options.ConfigureQueueServiceClient(DefaultAzuriteQueueCon); });
        }
    }
}