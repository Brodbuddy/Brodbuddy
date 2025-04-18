using Shouldly;

namespace SharedTestDependencies;

public static class ShouldlyDateTimeExtensions
{
    public static void ShouldBeWithinTolerance(this DateTime actual, DateTime expected, double toleranceInMilliseconds = 1)
    {
        Math.Abs((actual - expected).TotalMilliseconds).ShouldBeLessThan(toleranceInMilliseconds);
    }
}