using SharedTestDependencies.Constants;
using SharedTestDependencies.Database;

namespace Infrastructure.Data.Tests.Collections;

[CollectionDefinition(TestCollections.Database)]
public class DatabaseTestCollection : ICollectionFixture<PostgresFixture>;