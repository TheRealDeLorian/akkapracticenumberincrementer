using System.Collections.Immutable;
using Akka.Util.Internal;

namespace akkastreamsinclass;

public record BroadcastRequest();
public record BroadcastSubscriptionRequest(IActorRef subscriber);
public class TransformerActor : ReceiveActor, IWithTimers
{
    public ITimerScheduler Timers {get; set;}
    private ImmutableHashSet<IActorRef> subscribers = [];
    private int number = 0;
    public TransformerActor()
    {
        Receive<string>(str =>
        {
            Sender.Tell(str.ToUpperInvariant());
        });
        Receive<BroadcastRequest>(m => {
            number++; //no concurrency issues because single actors run single threadedly. 
            subscribers.ForEach(s => s.Tell(number));
        });
        Receive<BroadcastSubscriptionRequest>(m => {
            subscribers = [..subscribers, m.subscriber];
        });
    }

    protected override void PreStart()
    {
        Timers.StartPeriodicTimer("thing", new BroadcastRequest(), TimeSpan.FromSeconds(1));
    }
}