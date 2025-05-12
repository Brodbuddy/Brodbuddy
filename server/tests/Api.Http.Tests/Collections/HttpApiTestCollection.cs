using SharedTestDependencies.Constants;
using SharedTestDependencies.Fixtures;

namespace Api.Http.Tests.Collections;

[CollectionDefinition(TestCollections.HttpApi)]
public class HttpApiTestCollection : ICollectionFixture<PostgresFixture>;
