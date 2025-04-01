using Application;
using Application.Interfaces;
using FluentEmail.Core;
using FluentEmail.Core.Models;

namespace Infrastructure.Communication.Mail;

public class FluentEmailSender(IFluentEmail fluentEmail) : IEmailSender
{
    public async Task<bool> SendEmailAsync(string to, string topic, string content)
    {
        SendResponse response = await fluentEmail.To(to).Subject(topic).Body(content).Tag("aa").SendAsync();
        return response.Successful;
    }
}