using SharedTestDependencies;

namespace Infrastructure.Data.Tests;

[CollectionDefinition(TestCollections.Database)]
public class DatabaseTestCollection : ICollectionFixture<PostgresFixture>;