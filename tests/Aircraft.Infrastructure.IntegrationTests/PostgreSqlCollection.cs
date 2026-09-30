namespace Aircraft.Infrastructure.IntegrationTests;

[CollectionDefinition("Postgres")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>;
