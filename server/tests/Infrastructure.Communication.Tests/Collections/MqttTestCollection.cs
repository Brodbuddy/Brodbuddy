using SharedTestDependencies.Constants;
using SharedTestDependencies.Fixtures;

namespace Infrastructure.Communication.Tests.Collections;

[CollectionDefinition(TestCollections.Mqtt)]
public class MqttTestCollection : ICollectionFixture<VerneMqFixture>;