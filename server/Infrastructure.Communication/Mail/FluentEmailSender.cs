using Application.Interfaces.Communication.Mail;
using FluentEmail.Core;
using FluentEmail.Core.Models;

namespace Infrastructure.Communication.Mail;

public class FluentEmailSender(IFluentEmail fluentEmail) : IEmailSender
{
    public async Task<bool> SendEmailAsync(string recipient, string topic, string content)
    {
        SendResponse response = await fluentEmail.To(recipient).Subject(topic).Body(content).Tag("aa").SendAsync();
        return response.Successful;
    }
}