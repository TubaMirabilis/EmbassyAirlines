namespace Aircraft.Publisher.Lambda.IntegrationTests;

[CollectionDefinition("Postgres")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>;
