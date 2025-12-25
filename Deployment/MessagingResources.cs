using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Deployment;

internal sealed class MessagingResources : Construct
{
    internal MessagingResources(Construct scope, string id) : base(scope, id)
    {
        AircraftCreatedTopic = new Topic(this, "AircraftCreatedTopic");
        AirportCreatedTopic = new Topic(this, "AirportCreatedTopic");
        AirportUpdatedTopic = new Topic(this, "AirportUpdatedTopic");
        FlightScheduledTopic = new Topic(this, "FlightScheduledTopic");
        DeadLetterQueue = new Queue(this, "DeadLetterQueue");
        ProcessingQueue = new Queue(this, "ProcessingQueue", new QueueProps
        {
            DeadLetterQueue = new DeadLetterQueue
            {
                MaxReceiveCount = 3,
                Queue = DeadLetterQueue
            }
        });
        AircraftCreatedTopic.AddSubscription(new SqsSubscription(ProcessingQueue));
        AirportCreatedTopic.AddSubscription(new SqsSubscription(ProcessingQueue));
        AirportUpdatedTopic.AddSubscription(new SqsSubscription(ProcessingQueue));
    }
    internal Topic AircraftCreatedTopic { get; }
    internal Topic AirportCreatedTopic { get; }
    internal Topic AirportUpdatedTopic { get; }
    internal Topic FlightScheduledTopic { get; }
    internal Queue DeadLetterQueue { get; }
    internal Queue ProcessingQueue { get; }
}
