namespace FrameData.Api.IntegrationTests.Fixtures;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiPostgresCollection : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "ApiPostgres";
}
