using NodaTime.TimeZones;

namespace Flights.Api.Extensions;

internal static class ZoneLocalMappingResolverExtensions
{
    extension(ZoneLocalMappingResolver)
    {
        public static ZoneLocalMappingResolver FromSchedulingAmbiguityPolicy(SchedulingAmbiguityPolicy policy)
        {
            var ambiguousTimeResolver = policy switch
            {
                SchedulingAmbiguityPolicy.PreferEarlier => Resolvers.ReturnEarlier,
                SchedulingAmbiguityPolicy.PreferLater => Resolvers.ReturnLater,
                _ => Resolvers.ThrowWhenAmbiguous
            };
            var skippedTimeResolver = Resolvers.ThrowWhenSkipped;
            return Resolvers.CreateMappingResolver(ambiguousTimeResolver, skippedTimeResolver);
        }
    }
}
