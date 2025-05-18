using System.Text.RegularExpressions;
using Application.Interfaces.Communication.Mail;

namespace Startup.Tests.Infrastructure.Fakes;

public class FakeEmailSender : IEmailSender
{
    private readonly object _lock = new();
    private readonly Dictionary<string, List<EmailMessage>> _sentEmails = new();
    public bool SimulateFailure { get; set; }

    public Task<bool> SendEmailAsync(string recipient, string topic, string content)
    {
        if (SimulateFailure) return Task.FromResult(false);
            
        lock (_lock)
        {
            if (!_sentEmails.TryGetValue(recipient, out var emailList))
            {
                emailList = [];
                _sentEmails[recipient] = emailList;
            }
            
            emailList.Add(new EmailMessage(recipient, topic, content));
        }
        
        return Task.FromResult(true);
    }
    
    public IReadOnlyList<EmailMessage> GetEmailsFor(string recipient)
    {
        lock (_lock)
        {
            return _sentEmails.TryGetValue(recipient, out var emailList) ? emailList.ToList() : Array.Empty<EmailMessage>();
        }
    }
    
    public void ClearEmails()
    {
        lock (_lock)
        {
            _sentEmails.Clear();
        }
    }
    
    public int? GetLatestOtpFor(string email)
    {
        var emails = GetEmailsFor(email);
        if (emails.Count == 0)
            return null;
            
        var latestEmail = emails[^1];
        
        var match = Regex.Match(latestEmail.Content, @"\b(\d{6})\b");
        if (match.Success && int.TryParse(match.Groups[1].Value, out var code))
        {
            return code;
        }
        
        return null;
    }
    
    public record EmailMessage(string Recipient, string Topic, string Content);
}