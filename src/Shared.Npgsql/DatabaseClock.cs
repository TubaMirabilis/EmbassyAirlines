namespace Shared.Npgsql;

// Exposes PostgreSQL's own clock to LINQ queries. Calls are never executed in-process: they are mapped to the
// built-in now() function and evaluated by the database. Timing that coordinates concurrent workers, outbox
// lease expiry above all, has to be decided by the one clock every worker shares rather than by whichever
// machine happens to be issuing the statement.
public static class DatabaseClock
{
    public static DateTime UtcNow() => throw new NotSupportedException($"{nameof(DatabaseClock)}.{nameof(UtcNow)} may only be used inside an EF Core query, where it is translated to the PostgreSQL now() function.");
}
