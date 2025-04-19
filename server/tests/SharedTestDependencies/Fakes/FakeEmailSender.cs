using Application.Interfaces.Communication.Mail;

namespace SharedTestDependencies.Fakes;

public class FakeEmailSender : IEmailSender
{
    public bool SimulateFailure { get; set; }

    public Task<bool> SendEmailAsync(string recipient, string topic, string content)
    {
        return Task.FromResult(!SimulateFailure);
    }
}