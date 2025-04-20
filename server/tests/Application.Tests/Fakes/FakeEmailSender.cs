using Application.Interfaces.Communication.Mail;

namespace Application.Tests.Fakes;

public class FakeEmailSender : IEmailSender
{
    public bool SimulateFailure { get; set; }

    public Task<bool> SendEmailAsync(string recipient, string topic, string content)
    {
        return Task.FromResult(!SimulateFailure);
    }
}