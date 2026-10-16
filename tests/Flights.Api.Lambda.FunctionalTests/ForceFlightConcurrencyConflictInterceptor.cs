using Flights.Core.Models;
using Flights.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Flights.Api.Lambda.FunctionalTests;

internal sealed class ForceFlightConcurrencyConflictInterceptor
    : SaveChangesInterceptor
{
    private sealed record PendingConflict(
        Guid FlightId,
        Func<Guid, CancellationToken, Task> CompetingUpdate);

    private PendingConflict? _pending;

    public void Arm(
        Guid flightId,
        Func<Guid, CancellationToken, Task> competingUpdate)
    {
        var pending = new PendingConflict(flightId, competingUpdate);

        if (Interlocked.CompareExchange(
                ref _pending,
                pending,
                null) is not null)
        {
            throw new InvalidOperationException(
                "A concurrency conflict is already armed.");
        }
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not ApplicationDbContext db)
        {
            return result;
        }

        db.ChangeTracker.DetectChanges();

        var pending = Volatile.Read(ref _pending);

        if (pending is null)
        {
            return result;
        }

        var targetFlightModified = db.ChangeTracker
            .Entries<Flight>()
            .Any(entry =>
                entry.Entity.Id == pending.FlightId &&
                entry.State == EntityState.Modified);

        if (!targetFlightModified)
        {
            return result;
        }

        if (Interlocked.CompareExchange(
                ref _pending,
                null,
                pending) == pending)
        {
            await pending.CompetingUpdate(
                pending.FlightId,
                cancellationToken);
        }

        return result;
    }
}
