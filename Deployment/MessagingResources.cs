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
        FlightArrivedTopic = new Topic(this, "FlightArrivedTopic");
        FlightMarkedAsDelayedEnRouteTopic = new Topic(this, "FlightMarkedAsDelayedEnRouteTopic");
        FlightMarkedAsEnRouteTopic = new Topic(this, "FlightMarkedAsEnRouteTopic");
        FlightScheduledTopic = new Topic(this, "FlightScheduledTopic");
    }
    internal Topic AircraftCreatedTopic { get; }
    internal Topic AirportCreatedTopic { get; }
    internal Topic AirportUpdatedTopic { get; }
    internal Topic FlightArrivedTopic { get; }
    internal Topic FlightMarkedAsDelayedEnRouteTopic { get; }
    internal Topic FlightMarkedAsEnRouteTopic { get; }
    internal Topic FlightScheduledTopic { get; }
}
