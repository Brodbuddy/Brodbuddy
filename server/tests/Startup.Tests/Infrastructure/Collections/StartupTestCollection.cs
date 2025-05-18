using SharedTestDependencies.Constants;
using Startup.Tests.Infrastructure.Fixtures;

namespace Startup.Tests.Infrastructure.Collections;

[CollectionDefinition(TestCollections.Startup)]
public class StartupTestCollection : ICollectionFixture<StartupTestFixture>;