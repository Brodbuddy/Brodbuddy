namespace Application.Interfaces.Communication.Mail;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string to, string topic, string content);
}