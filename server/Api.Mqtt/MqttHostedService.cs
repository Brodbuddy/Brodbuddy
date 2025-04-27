using HiveMQtt.Client;
using HiveMQtt.Client.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Mqtt;

public class MqttHostedService : IHostedService, IAsyncDisposable
{
    private readonly HiveMQClient _mqttClient;
    private readonly MqttDispatcher _dispatcher;
    private readonly ILogger<MqttHostedService> _logger;
    private bool _isDisposed;

    public MqttHostedService(
        HiveMQClient mqttClient,
        MqttDispatcher dispatcher,
        ILogger<MqttHostedService> logger)
    {
        _mqttClient = mqttClient;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting MQTT Hosted Service");

        if (!_mqttClient.IsConnected())
        {
            await ConnectWithRetryAsync(cancellationToken);
        }

        _mqttClient.OnMessageReceived += HandleMessageReceived;

        await SubscribeToTopicsAsync();

        _logger.LogInformation("MQTT Hosted Service started successfully");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping MQTT Hosted Service");

        _mqttClient.OnMessageReceived -= HandleMessageReceived;

        if (_mqttClient.IsConnected())
        {
            try
            {
                await _mqttClient.DisconnectAsync();
                _logger.LogInformation("MQTT client disconnected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting MQTT client");
            }
        }
    }

    private async Task ConnectWithRetryAsync(CancellationToken cts)
    {
        const int maxRetries = 5;
        for (int i = 1; i <= maxRetries; i++)
        {
            try
            {
                _logger.LogInformation("Attempting to connect to MQTT broker (attempt {Attempt}/{MaxRetries})", i, maxRetries);

                var result = await _mqttClient.ConnectAsync();
                if (result.ReasonCode != HiveMQtt.MQTT5.ReasonCodes.ConnAckReasonCode.Success)
                {
                    _logger.LogError("Failed to connect to MQTT broker: {Reason}", result.ReasonString);

                    if (i == maxRetries)
                    {
                        throw new InvalidOperationException($"Failed to connect to MQTT broker after {maxRetries} attempts: {result.ReasonString}");
                    }
                }
                else
                {
                    _logger.LogInformation("Successfully connected to MQTT broker");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to MQTT broker on attempt {Attempt}", i);

                if (i == maxRetries)
                {
                    throw;
                }
            }

            // Venter før vi prøver igen - eksponentielt backoff
            int delay = (int) Math.Pow(2, i);
            _logger.LogInformation("Waiting {Delay} seconds before retry", delay);
            await Task.Delay(TimeSpan.FromSeconds(delay), cts);
        }
    }

    private async Task SubscribeToTopicsAsync()
    {
        var subscriptions = _dispatcher.GetSubscriptions().ToList();

        if (subscriptions.Count == 0)
        {
            _logger.LogWarning("No MQTT subscriptions found - no topics will be subscribed to");
            return;
        }

        _logger.LogInformation("Subscribing to {Count} MQTT topics", subscriptions.Count);

        var builder = new SubscribeOptionsBuilder();
        foreach (var (topicFilter, qos) in subscriptions)
        {
            _logger.LogDebug("Adding subscription for topic {TopicFilter} with QoS {QoS}", topicFilter, qos);
            builder.WithSubscription(topicFilter, qos);
        }

        try
        {
            var result = await _mqttClient.SubscribeAsync(builder.Build());
            _logger.LogInformation("Successfully subscribed to {Count} MQTT topics", result.Subscriptions.Count);

            var failedSubscriptions = result.Subscriptions.Where(s =>
                s.SubscribeReasonCode != HiveMQtt.MQTT5.ReasonCodes.SubAckReasonCode.GrantedQoS0 &&
                s.SubscribeReasonCode != HiveMQtt.MQTT5.ReasonCodes.SubAckReasonCode.GrantedQoS1 &&
                s.SubscribeReasonCode != HiveMQtt.MQTT5.ReasonCodes.SubAckReasonCode.GrantedQoS2).ToList();

            if (failedSubscriptions.Count != 0)
            {
                foreach (var sub in failedSubscriptions)
                {
                    _logger.LogError("Failed to subscribe to topic {Topic}: {Reason}", sub.TopicFilter.Topic,
                        sub.SubscribeReasonCode);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing to MQTT topics");
            throw new InvalidOperationException("Error subscribing to MQTT topics");
        }
    }

    private void HandleMessageReceived(object? sender, OnMessageReceivedEventArgs args)
    {
        // Bruger Task.Run for at undgå at blokere klientens event handler tråd
        // og at undgå async void metode problemer
        _ = Task.Run(async () =>
        {
            try
            {
                await _dispatcher.DispatchAsync(args);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception in MQTT message processing");
            }
        });
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
            return;

        if (_mqttClient.IsConnected())
        {
            try
            {
                await _mqttClient.DisconnectAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during MQTT client disposal");
            }
        }

        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}