namespace Application.Interfaces.Communication.Mail;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string recipient, string topic, string content);
}