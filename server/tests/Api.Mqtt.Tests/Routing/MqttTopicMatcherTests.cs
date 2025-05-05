using Api.Mqtt.Routing;
using Shouldly;
using Xunit.Abstractions;

namespace Api.Mqtt.Tests.Routing;

public class MqttTopicMatcherTests
{
    private readonly ITestOutputHelper _output;

    public MqttTopicMatcherTests(ITestOutputHelper output)
    {
        _output = output;
    }
    
    public class TopicMatches : MqttTopicMatcherTests
    {
        public TopicMatches(ITestOutputHelper output) : base(output)
        {
        }
        // Hjælpe metode for at mindske duplikatkode.
        private static void AssertTopicMatches(string filter, string topic, bool expectedResult)
        {
            // Act 
            bool result = MqttTopicMatcher.Matches(filter, topic);

            // Assert
            result.ShouldBe(expectedResult);
        }

        [Theory]
        [InlineData("test/topic", "test/topic", true)]
        [InlineData("test/topic", "different/topic", false)]
        [InlineData("test/topic", "test/topic/sensors", false)]
        [InlineData("test/topic/", "test/topic", false)]
        public void Matches_ExactTopics_ShouldMatchOnlyIdenticalTopics(string filter, string topic, bool expectedResult)
        {
            AssertTopicMatches(filter, topic, expectedResult);
        }

        [Theory]
        [InlineData("test/+/topic", "test/middle/topic", true)]
        [InlineData("test/+/topic", "test/something/topic", true)]
        [InlineData("test/+/topic", "test/topic", false)]
        [InlineData("test/+/topic", "test/something/extra/topic", false)]
        [InlineData("+/topic", "prefix/topic", true)]
        [InlineData("+/topic", "topic", false)]
        [InlineData("test/+", "test/hello", true)]
        [InlineData("+/+/+", "a/b/c", true)]
        [InlineData("+/+/+", "a/b/c/", false)]
        [InlineData("+/+/+", "a/b/c/d", false)]
        public void Matches_SingleLevelWildCard_ShouldMatchCorrectly(string filter, string topic, bool expectedResult)
        {
            AssertTopicMatches(filter, topic, expectedResult);
        }

        [Theory]
        [InlineData("test/#", "test", true)]
        [InlineData("test/#", "test/something", true)]
        [InlineData("test/#", "test/a/b/c/d", true)]
        [InlineData("test/topic/#", "test/topic", true)]
        [InlineData("test/topic/#", "test/topic/a/b/c/d", true)]
        [InlineData("test/#", "a/test", false)]
        [InlineData("#", "matches/any/topic", true)]
        [InlineData("#", "", true)]
        public void Matches_MultiLevelWildcard_ShouldMatchCorrectly(string filter, string topic, bool expectedResult)
        {
            AssertTopicMatches(filter, topic, expectedResult);
        }

        [Theory]
        [InlineData("devices/+/telemetry", "devices/dev1/telemetry", true)]
        [InlineData("devices/+/telemetry", "devices/sensors/telemetry", true)]
        [InlineData("devices/+/telemetry", "devices/telemetry", false)]
        [InlineData("devices/+/telemetry", "services/dev1/telemetry", false)]

        public void Matches_DeviceTopicPattern_ShouldMatchCorrectly(string filter, string topic, bool expectedResult)
        {
            AssertTopicMatches(filter, topic, expectedResult);
        }

        [Theory]
        [InlineData("topic/+/#", "topic/something/anything", true)]
        [InlineData("topic/+/#", "topic/something", true)]
        [InlineData("topic/+/#", "topic", false)]
        [InlineData("+/+/#", "topic/something", true)]
        [InlineData("+/+/#", "topic/something/anything", true)]
        [InlineData("+/+/#", "topic/something/anything/sensors", true)]
        public void Matches_CombinedWildCards_ShouldMatchCorrectly(string filter, string topic, bool expectedResult)
        {
            AssertTopicMatches(filter, topic, expectedResult);
        }

        [Theory]
        [InlineData(null, "topic/something/anything", false)]
        [InlineData("topic/+/#", null, false)]
        [InlineData(null, null, false)]
        [InlineData("", "topic/something/anything", false)]
        [InlineData("topic/+", "", false)]
        [InlineData("", "", false)]
        public void Matches_NullOrEmptyInputs_ShouldReturnFalse(string? filter, string? topic, bool expectedResult)
        {
            if (filter != null && topic != null) AssertTopicMatches(filter, topic, expectedResult);
        }
    }
}