using Orleans;

namespace OrleansStreamingDemo.Grains.Interfaces;

public interface IProducerGrain : IGrainWithStringKey
{
   Task StartProducing(string streamNameSpace, Guid key);

   Task StopProducing();
}