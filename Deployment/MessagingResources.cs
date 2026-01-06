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
        AircraftAssignedToFlightTopic = new Topic(this, "AircraftAssignedToFlightTopic");
        FlightPricingAdjustedTopic = new Topic(this, "FlightPricingAdjustedTopic");
        FlightRescheduledTopic = new Topic(this, "FlightRescheduledTopic");
        FlightCancelledTopic = new Topic(this, "FlightCancelledTopic");
        FlightDelayedTopic = new Topic(this, "FlightDelayedTopic");
    }
    internal Topic AircraftCreatedTopic { get; }
    internal Topic AirportCreatedTopic { get; }
    internal Topic AirportUpdatedTopic { get; }
    internal Topic FlightArrivedTopic { get; }
    internal Topic FlightMarkedAsDelayedEnRouteTopic { get; }
    internal Topic FlightMarkedAsEnRouteTopic { get; }
    internal Topic FlightScheduledTopic { get; }
    internal Topic AircraftAssignedToFlightTopic { get; }
    internal Topic FlightPricingAdjustedTopic { get; }
    internal Topic FlightRescheduledTopic { get; }
    internal Topic FlightCancelledTopic { get; }
    internal Topic FlightDelayedTopic { get; }
}
