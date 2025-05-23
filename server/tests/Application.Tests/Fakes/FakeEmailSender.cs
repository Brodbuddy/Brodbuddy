using Application.Interfaces.Communication.Mail;

namespace Application.Tests.Fakes;

public class FakeEmailSender : IEmailSender
{
    public bool SimulateFailure { get; set; }

    public Task<bool> SendVerificationCodeAsync(string recipient, string subject, string code)
    {
        return Task.FromResult(!SimulateFailure);
    }
}