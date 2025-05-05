using SharedTestDependencies.Constants;
using SharedTestDependencies.Database;

namespace Api.Http.Tests.Collections;

[CollectionDefinition(TestCollections.HttpApi)]
public class HttpApiTestCollection : ICollectionFixture<PostgresFixture>;
