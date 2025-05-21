using Core.Interfaces;

namespace Core.ValueObjects;

public record SourdoughReading(double RisePercent, double TemperatureCelsius, double HumidityPercent, DateTime Timestamp) : IBroadcastMessage;