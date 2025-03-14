using System.Drawing.Text;
using Akka.Hosting;
using Akka.Streams;
using akkastreamsinclass;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static akkastreamsinclass.TransformerActor;
CancellationTokenSource _shutdownCts = new();

var hostBuilder = new HostBuilder();

hostBuilder.ConfigureServices((context, services) =>
{
    services.AddAkka("MyActorSystem", (builder, sp) =>
    {
        builder
            .WithActors((system, registry, resolver) =>
            {
                var helloActor = system.ActorOf(Props.Create(() => new TransformerActor()), "transformer");
                registry.Register<TransformerActor>(helloActor);
            });
    });
});

var host = hostBuilder.Build();

var completionTask = host.RunAsync();

// grab the ActorSystem from the DI container
var system = host.Services.GetRequiredService<ActorSystem>();

// grab the ActorRef from the DI container
IActorRef transformer = host.Services.GetRequiredService<IRequiredActor<TransformerActor>>().ActorRef;

var source = Source.ActorRef<int>(100, OverflowStrategy.DropHead);
var (srcActor, src) = source.PreMaterialize(system);

//subscribes
transformer.Tell(new BroadcastSubscriptionRequest(srcActor));

await foreach (var number in src
                    .RunAsAsyncEnumerable(system)
                    .WithCancellation(_shutdownCts.Token))
{
    Console.WriteLine(number);
}

// shut the actor down to prevent resource leaks
srcActor.Tell(PoisonPill.Instance);

await completionTask; // wait for the host to shut down