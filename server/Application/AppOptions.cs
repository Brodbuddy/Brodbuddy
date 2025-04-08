

namespace Application;

public class AppOptions
{
    public int HttpPort { get; set; } = 5001;
    public EmailOptions Email { get; set; } = new();
    public PostgresOptions Postgres { get; set; } = new();
    
    public JwtOptions Jwt { get; set; } = new();
}

public class EmailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string Sender { get; set; } = "Brian Petersen";
    public string FromEmail { get; set; } = "TestMail@test.dk";
}

public class PostgresOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "db";
    public string Username { get; set; } = "user";
    public string Password { get; set; } = "pass";

    public string ConnectionString =>
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}

public class JwtOptions
{
    public string Secret { get; set; } = "dfKDL0Rq26AEQhdHBcQkOvMNjj9S8/thdKhTVzm3UDWXfJ0gePCuWyf48VK9/hk1ID4VHqZjXpYhinms1r+Khg==";
    public int ExpirationMinutes { get; set; } = 15;
    public string Issuer { get; set; } = "localhost:5001";
    public string Audience { get; set; } = "localhost:5173";
}