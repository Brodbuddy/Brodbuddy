using SharedTestDependencies.Constants;
using SharedTestDependencies.Fixtures;

namespace Infrastructure.Data.Tests.Collections;

[CollectionDefinition(TestCollections.Database)]
public class DatabaseTestCollection : ICollectionFixture<PostgresFixture>;