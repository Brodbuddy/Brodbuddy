namespace Startup.TcpProxy;

public static class TcpProxyExtensions
{
    public static IServiceCollection AddTcpProxyService(this IServiceCollection services)
    {
        services.AddHostedService<TcpProxyHostedService>();
        return services;
    }
}