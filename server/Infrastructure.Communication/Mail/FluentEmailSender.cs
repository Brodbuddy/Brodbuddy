using Application.Interfaces.Communication.Mail;
using FluentEmail.Core;
using FluentEmail.Core.Models;
using Microsoft.Extensions.Logging;
using RazorLight;

namespace Infrastructure.Communication.Mail;

public class FluentEmailSender : IEmailSender
{
    private readonly IFluentEmail _fluentEmail;
    private readonly ILogger<FluentEmailSender> _logger;
    private readonly RazorLightEngine _razorEngine;
    
    private const string VerificationEmailTemplate = "PasswordlessCodeLink.cshtml";
    private const string VerificationTag = "verification";

    public FluentEmailSender(IFluentEmail fluentEmail, ILogger<FluentEmailSender> logger)
    {
        _fluentEmail = fluentEmail;
        _logger = logger;
        
        var templatePath = GetTemplatesPath();
        
        if (!Directory.Exists(templatePath))
        {
            throw new DirectoryNotFoundException($"Template directory not found at: {templatePath}");
        }
        
        _razorEngine = new RazorLightEngineBuilder()
            .UseFileSystemProject(templatePath)
            .UseMemoryCachingProvider()
            .Build();
    }
    
    private static string GetTemplatesPath()
    {
        var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mail", "Templates");
        
        if (Directory.Exists(templatePath))
        {
            return templatePath;
        }
        
        var rootPath = Directory.GetCurrentDirectory();
        templatePath = Path.GetFullPath(Path.Combine(rootPath, "..", "Infrastructure.Communication", "Mail", "Templates"));
        
        return templatePath;
    }

    public async Task<bool> SendVerificationCodeAsync(string recipient, string subject, string code)
    {
        try
        {
            var verificationUrl = $"https://brodbuddy.com/verify?code={code}";
            var model = new { Code = code, VerificationUrl = verificationUrl };
            var template = await _razorEngine.CompileRenderAsync(VerificationEmailTemplate, model);
            
            var response = await _fluentEmail
                .To(recipient)
                .Subject(subject)
                .Body(template, isHtml: true)
                .Tag(VerificationTag)
                .SendAsync();
                
            if (response.Successful)
            {
                _logger.LogInformation("Verification email sent successfully to {Email}", MaskEmail(recipient));
            }
            else
            {
                _logger.LogError("Failed to send verification email to {Email}: {Errors}", 
                    MaskEmail(recipient), string.Join(", ", response.ErrorMessages));
            }
            
            return response.Successful;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", MaskEmail(recipient));
            return false;
        }
    }
    
    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return "***";
        
        var localPart = parts[0];
        var domain = parts[1];
        
        if (localPart.Length <= 2) return $"{localPart}@{domain}";
        
        var masked = localPart[0] + new string('*', Math.Min(localPart.Length - 2, 5)) + localPart[^1];
        return $"{masked}@{domain}";
    }
}