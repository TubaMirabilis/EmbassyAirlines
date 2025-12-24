using Amazon.CDK.AWS.SNS;
using Constructs;

namespace Deployment;

internal sealed class MessagingResources : Construct
{
    internal MessagingResources(Construct scope, string id) : base(scope, id)
    {
        AircraftCreatedTopic = new Topic(this, "AircraftCreatedTopic");
        AirportCreatedTopic = new Topic(this, "AirportCreatedTopic");
        AirportUpdatedTopic = new Topic(this, "AirportUpdatedTopic");
    }
    internal Topic AircraftCreatedTopic { get; }
    internal Topic AirportCreatedTopic { get; }
    internal Topic AirportUpdatedTopic { get; }
}
