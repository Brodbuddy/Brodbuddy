using SharedTestDependencies.Constants;

namespace Startup.Tests.Collections;


[CollectionDefinition(TestCollections.Startup)]
public class StartupTestCollection : ICollectionFixture<StartupTestFixture>;