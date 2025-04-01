namespace Application.Interfaces;

public interface IEmailSender
{
    Task<bool> SendEmailAsync(string to, string topic, string content);
}