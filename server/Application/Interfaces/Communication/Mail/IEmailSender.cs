namespace Application.Interfaces.Communication.Mail;

public interface IEmailSender
{
    Task<bool> SendVerificationCodeAsync(string recipient, string subject, string code);
}