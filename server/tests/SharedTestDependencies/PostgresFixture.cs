using System.Data.Common;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SharedTestDependencies;

public class PostgresFixture :  DbContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>
{
    private readonly INetwork _network;
    private readonly IMessageSink _messageSink;

    private Respawner? Respawner { get; set; }

    public PostgresFixture(IMessageSink messageSink) : base(messageSink)
    {
        _messageSink = messageSink; 
        _network = new NetworkBuilder().WithName($"postgres-net-{Guid.NewGuid()}").Build();
    }

    protected override PostgreSqlBuilder Configure(PostgreSqlBuilder builder)
    {
        return builder.WithImage("postgres:16-alpine")
            .WithDatabase("db")
            .WithUsername("user")
            .WithPassword("pass")
            .WithNetwork(_network)
            .WithNetworkAliases("postgres-db");
    }

    public override DbProviderFactory DbProviderFactory => NpgsqlFactory.Instance;

    protected override async Task InitializeAsync()
    {
        Log("Starting PostgreSQL container...");
        await base.InitializeAsync();
        Log("PostgreSQL container started. Connection string: " + ConnectionString);

        await ApplyDatabaseSchemaAsync();

        Log("Initializing Respawner...");
        await InitializeRespawnerAsync();
        Log("Respawner initialized successfully.");
    }

    private async Task ApplyDatabaseSchemaAsync()
    {
        Log($"Applying database schema to: {ConnectionString}");

        IContainer? flywayContainer = null;
        try
        {
            var solutionDir = FindSolutionDirectory();
            var migrationsDir = Path.Combine(solutionDir, "db", "migrations");

            if (!Directory.Exists(migrationsDir)) throw new DirectoryNotFoundException($"Migrations directory not found: {migrationsDir}");

            flywayContainer = new ContainerBuilder().WithImage("flyway/flyway:10-alpine")
                .WithResourceMapping(migrationsDir, "/flyway/sql")
                .WithNetwork(_network)
                .WithEntrypoint("flyway")
                .WithCommand(
                    "-url=jdbc:postgresql://postgres-db:5432/db",
                    "-user=user",
                    "-password=pass",
                    "-connectRetries=10",
                    "-X",
                    "migrate"
                )
                .Build();

            await flywayContainer.StartAsync();
            var exitCode = await flywayContainer.GetExitCodeAsync();

            if (exitCode != 0)
            {
                var stdout = "N/A";
                string stderr;
                try
                {
                    var logs = await flywayContainer.GetLogsAsync();
                    stdout = logs.Stdout;
                    stderr = logs.Stderr;
                }
                catch (Exception logEx)
                {
                    stderr = $"Failed to get Flyway logs: {logEx.Message}";
                }

                throw new Exception($"Flyway migration failed with exit code {exitCode}.\nStdOut:\n{stdout}\nStdErr:\n{stderr}");
            }

            await flywayContainer.StopAsync();
            await flywayContainer.DisposeAsync();

            Log("Database schema applied successfully via Flyway.");
        }
        catch (Exception ex)
        {
            Log($"Error applying database schema: {ex.Message}");
            throw;
        }
        finally
        {
            if (flywayContainer != null)
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await flywayContainer.StopAsync(cts.Token);
                }
                catch (Exception stopEx)
                {
                    Log($"Error stopping Flyway container: {stopEx.Message}");
                }
                finally
                {
                    await flywayContainer.DisposeAsync();
                    Log("Flyway container disposed");
                }
            }
        }
    }

    private static string FindSolutionDirectory()
    {
        var directory = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (directory != null && directory.GetFiles("*.sln").Length == 0)
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not find solution directory.");
    }

    private async Task InitializeRespawnerAsync()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        Respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = ["public"],
            TablesToIgnore = ["flyway_schema_history"]
        });
        Log("Respawner instance created");
    }

    public async Task ResetDatabaseAsync()
    {
        if (Respawner == null) throw new InvalidOperationException("Respawner has not been initialized.");

        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        await Respawner.ResetAsync(connection);
    }

    protected override async Task DisposeAsyncCore()
    {
        await base.DisposeAsyncCore();
        await _network.DisposeAsync();
    }

    private void Log(string message)
    {
        _messageSink.OnMessage(new DiagnosticMessage(message));
    }
}