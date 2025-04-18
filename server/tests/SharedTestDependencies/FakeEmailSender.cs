using Application.Interfaces;

namespace SharedTestDependencies;

public class FakeEmailSender : IEmailSender
{
    public bool SimulateFailure { get; set; } = false;

    public Task<bool> SendEmailAsync(string to, string topic, string content)
    {
        return Task.FromResult(!SimulateFailure);
    }
}