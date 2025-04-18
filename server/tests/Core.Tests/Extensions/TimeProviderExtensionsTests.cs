using Core.Extensions;
using SharedTestDependencies;
using Shouldly;

namespace Core.Tests.Extensions;

public class TimeProviderExtensionsTests
{
    [Fact]
    public void Tomorrow_ShouldReturnDateTimeExactlyOneDayAfterNow()
    {
        // Arrange
        var startTime = new DateTimeOffset(2024, 5, 15, 10, 30, 0, TimeSpan.Zero);
        var timeProvider = new FakeTimeProvider(startTime);

        var expectedNow = startTime.UtcDateTime;
        var expectedTomorrow = expectedNow.AddDays(1);

        // Act
        var actualNow = timeProvider.Now();
        var actualTomorrow = timeProvider.Tomorrow(); 

        // Assert
        actualNow.ShouldBe(expectedNow); 
        actualTomorrow.ShouldBe(expectedTomorrow); 
        (actualTomorrow - actualNow).ShouldBe(TimeSpan.FromDays(1));
    }
}