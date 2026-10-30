using NodaTime;
using NodaTime.TimeZones;
using Shared;
using Shared.Contracts;

namespace Flights.Core.Models;

public sealed class Flight : Entity
{
    private Flight(FlightCreationArgs args)
    {
        Ensure.NotNullOrEmpty(args.FlightNumberIata);
        Ensure.NotNullOrEmpty(args.FlightNumberIcao);
        Id = Guid.NewGuid();
        CreatedAt = args.CreatedAt;
        UpdatedAt = CreatedAt;
        FlightNumberIata = args.FlightNumberIata;
        FlightNumberIcao = args.FlightNumberIcao;
        Status = FlightStatus.Scheduled;
        DepartureLocalTime = args.Schedule.DepartureTime;
        ArrivalLocalTime = args.Schedule.ArrivalTime;
        DepartureAirport = args.Schedule.DepartureAirport;
        ArrivalAirport = args.Schedule.ArrivalAirport;
        Aircraft = args.Aircraft;
        EconomyPrice = args.EconomyPrice;
        BusinessPrice = args.BusinessPrice;
        SchedulingAmbiguityPolicy = args.Schedule.SchedulingAmbiguityPolicy;
        OperationType = args.OperationType;
        AddDomainEvent(new FlightScheduledEvent(Guid.NewGuid(), Id, OperationType.ToString(), BusinessPrice.Amount, EconomyPrice.Amount));
    }
#pragma warning disable CS8618
    private Flight()
    {
    }
#pragma warning restore CS8618
    public Guid Id { get; init; }
    public Instant CreatedAt { get; init; }
    public Instant UpdatedAt { get; private set; }
    public string FlightNumberIata { get; private set; }
    public string FlightNumberIcao { get; private set; }
    public FlightStatus Status { get; private set; }
    public Instant? OffBlockAt { get; private set; }
    public Instant? TakeoffAt { get; private set; }
    public Instant? LandingAt { get; private set; }
    public Instant? OnBlockAt { get; private set; }
    public LocalDateTime DepartureLocalTime { get; private set; }
    public LocalDateTime ArrivalLocalTime { get; private set; }
    public SchedulingAmbiguityPolicy SchedulingAmbiguityPolicy { get; private set; }
    public ZonedDateTime DepartureZonedTime
    {
        get
        {
            var resolver = ZoneLocalMappingResolver.FromSchedulingAmbiguityPolicy(SchedulingAmbiguityPolicy);
            return DepartureLocalTime.InZone(DepartureAirport.TimeZone, resolver);
        }
    }
    public Aircraft Aircraft { get; private set; }
    public Airport ArrivalAirport { get; init; }
    public Instant ArrivalInstant => ArrivalZonedTime.ToInstant();
    public ZonedDateTime ArrivalZonedTime
    {
        get
        {
            var resolver = ZoneLocalMappingResolver.FromSchedulingAmbiguityPolicy(SchedulingAmbiguityPolicy);
            return ArrivalLocalTime.InZone(ArrivalAirport.TimeZone, resolver);
        }
    }
    public Money BusinessPrice { get; private set; }
    public Airport DepartureAirport { get; init; }
    public Instant DepartureInstant => DepartureZonedTime.ToInstant();
    public Money EconomyPrice { get; private set; }
    public OperationType OperationType { get; init; }
    public static Flight Create(FlightCreationArgs args) => new(args);
    public void AssignAircraft(Aircraft aircraft, Instant instant)
    {
        Aircraft = aircraft;
        UpdatedAt = instant;
        AddDomainEvent(new AircraftAssignedToFlightEvent(Guid.NewGuid(), Id, aircraft.Id));
    }
    public void AdjustPricing(Money economyPrice, Money businessPrice, Instant instant)
    {
        BusinessPrice = businessPrice;
        EconomyPrice = economyPrice;
        UpdatedAt = instant;
        AddDomainEvent(new FlightPricingAdjustedEvent(Guid.NewGuid(), Id, economyPrice.Amount, businessPrice.Amount));
    }
    public void Reschedule(FlightSchedule schedule, Instant instant)
    {
        ArrivalLocalTime = schedule.ArrivalTime;
        DepartureLocalTime = schedule.DepartureTime;
        SchedulingAmbiguityPolicy = schedule.SchedulingAmbiguityPolicy;
        UpdatedAt = instant;
        AddDomainEvent(new FlightRescheduledEvent(Guid.NewGuid(), Id, schedule.DepartureTime.ToDateTimeUnspecified(), schedule.ArrivalTime.ToDateTimeUnspecified()));
    }
    public void RecordMilestone(
        FlightMilestone milestone,
        Instant occurredAt,
        Instant updatedAt)
    {
        switch (milestone)
        {
            case FlightMilestone.OffBlock:
                ValidateOffBlock();
                EnsureStatusTransitionAllowed(FlightStatus.EnRoute);
                OffBlockAt = occurredAt;
                AdjustStatus(FlightStatus.EnRoute, updatedAt);
                break;

            case FlightMilestone.Takeoff:
                EnsureEnRoute(milestone);
                ValidateTakeoff(occurredAt);
                TakeoffAt = occurredAt;
                UpdatedAt = updatedAt;
                break;

            case FlightMilestone.Landing:
                EnsureEnRoute(milestone);
                ValidateLanding(occurredAt);
                LandingAt = occurredAt;
                UpdatedAt = updatedAt;
                break;

            case FlightMilestone.OnBlock:
                EnsureEnRoute(milestone);
                ValidateOnBlock(occurredAt);
                EnsureStatusTransitionAllowed(FlightStatus.Arrived);
                OnBlockAt = occurredAt;
                AdjustStatus(FlightStatus.Arrived, updatedAt);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(milestone));
        }
    }
    public void MarkAsDelayed(Instant occurredAt) => AdjustStatus(FlightStatus.Delayed, occurredAt);
    public void Cancel(Instant occurredAt) => AdjustStatus(FlightStatus.Cancelled, occurredAt);
    private void AdjustStatus(FlightStatus status, Instant updatedAt)
    {
        EnsureStatusTransitionAllowed(status);

        Status = status;
        UpdatedAt = updatedAt;

        AddDomainEvent(FlightStatusEventFactory.Create(this, status));
    }
    private void EnsureStatusTransitionAllowed(FlightStatus newStatus)
    {
        if (!FlightStatusTransitions.CanTransition(Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Cannot transition flight from {Status} to {newStatus}.");
        }
    }
    private void ValidateOffBlock()
    {
        if (Status is not (FlightStatus.Scheduled or FlightStatus.Delayed))
        {
            throw new InvalidOperationException(
                $"Cannot record OffBlock for a flight with status {Status}. " +
                "The flight must be Scheduled or Delayed.");
        }
        if (OffBlockAt is not null)
        {
            throw new InvalidOperationException(
                "The OffBlock milestone has already been recorded.");
        }
    }
    private void ValidateTakeoff(Instant occurredAt)
    {
        if (OffBlockAt is null)
        {
            throw new InvalidOperationException(
                "Cannot record Takeoff before the OffBlock milestone.");
        }
        if (TakeoffAt is not null)
        {
            throw new InvalidOperationException(
                "The Takeoff milestone has already been recorded.");
        }
        if (occurredAt < OffBlockAt.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "The Takeoff timestamp cannot be earlier than the OffBlock timestamp.");
        }
    }
    private void ValidateLanding(Instant occurredAt)
    {
        if (TakeoffAt is null)
        {
            throw new InvalidOperationException(
                "Cannot record Landing before the Takeoff milestone.");
        }
        if (LandingAt is not null)
        {
            throw new InvalidOperationException(
                "The Landing milestone has already been recorded.");
        }
        if (occurredAt < TakeoffAt.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "The Landing timestamp cannot be earlier than the Takeoff timestamp.");
        }
    }
    private void ValidateOnBlock(Instant occurredAt)
    {
        if (LandingAt is null)
        {
            throw new InvalidOperationException(
                "Cannot record OnBlock before the Landing milestone.");
        }
        if (OnBlockAt is not null)
        {
            throw new InvalidOperationException(
                "The OnBlock milestone has already been recorded.");
        }
        if (occurredAt < LandingAt.Value)
        {
            throw new ArgumentOutOfRangeException(
                nameof(occurredAt),
                occurredAt,
                "The OnBlock timestamp cannot be earlier than the Landing timestamp.");
        }
    }
    private void EnsureEnRoute(FlightMilestone milestone)
    {
        if (Status is not FlightStatus.EnRoute)
        {
            throw new InvalidOperationException(
                $"Cannot record {milestone} for a flight " +
                $"with status {Status}. The flight must be EnRoute.");
        }
    }
}
