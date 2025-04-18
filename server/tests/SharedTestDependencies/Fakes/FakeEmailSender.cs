using Application.Interfaces;

namespace SharedTestDependencies.Fakes;

public class FakeEmailSender : IEmailSender
{
    public bool SimulateFailure { get; set; }

    public Task<bool> SendEmailAsync(string to, string topic, string content)
    {
        return Task.FromResult(!SimulateFailure);
    }
}