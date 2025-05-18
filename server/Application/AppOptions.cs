namespace Application;

public class AppOptions
{
    public int PublicPort { get; set; } = 9999;
    public HttpOptions Http { get; init; } = new();
    public WebsocketOptions Websocket { get; init; } = new();
    public CorsOptions Cors { get; init; } = new();
    public EmailOptions Email { get; init; } = new();
    public PostgresOptions Postgres { get; init; } = new();
    public DragonflyOptions Dragonfly { get; init; } = new();
    public MqttOptions Mqtt { get; init; } = new();
    public JwtOptions Jwt { get; init; } = new();
    public SeqOptions Seq { get; init; } = new();
    public ZipkinOptions Zipkin { get; init; } = new();
    public TokenOptions Token { get; init; } = new();
}

public class HttpOptions
{
    public int Port { get; set; } = 5001;
}

public class WebsocketOptions
{
    public int Port { get; set; } = 8181;
}

public class CorsOptions
{
    public string[] AllowedOrigins { get; set; } = ["http://localhost:5173"];
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

    public string ConnectionString => $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}

public class DragonflyOptions
{
    public string ConnectionString { get; set; } = "localhost:6379";
    public bool AllowAdmin { get; set; } = true;
    public bool AbortOnConnectFail { get; set; }
}

public class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int MqttPort { get; set; } = 1883;
    public int WebSocketPort { get; set; } = 8080;
    public string Username { get; set; } = "user";
    public string Password { get; set; } = "pass";
}

public class JwtOptions
{
    public string Secret { get; set; } = "dfKDL0Rq26AEQhdHBcQkOvMNjj9S8/thdKhTVzm3UDWXfJ0gePCuWyf48VK9/hk1ID4VHqZjXpYhinms1r+Khg==";
    public int ExpirationMinutes { get; set; } = 1;
    public string Issuer { get; set; } = "localhost:5001";
    public string Audience { get; set; } = "localhost:5173";
}

public class SeqOptions
{
    public string ServerUrl { get; set; } = "http://localhost:5341";
    public string? ApiKey { get; set; }
}

public class ZipkinOptions
{
    public string? Endpoint { get; set; } = "http://localhost:9411/api/v2/spans"; 
    public double SamplingRate { get; set; } = 0.1; // 10%
}

public class TokenOptions
{
    public int RefreshTokenLifeTimeDays { get; set; } = 30;
}