using System.Text.Json;
using System.Text.RegularExpressions;

namespace PlaywrightTests.Helpers;

public class MailHogClient : IDisposable
{
    private readonly HttpClient _httpClient = new();
    private readonly string _mailhogBaseUrl;

    public MailHogClient(string baseUrl)
    {
        _mailhogBaseUrl = baseUrl + "/mailhog";
    }

    public async Task<string?> GetLatestVerificationCodeAsync(string email, int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            var code = await TryGetVerificationCodeAsync(email);
            if (code != null) return code;
            
            if (i < maxRetries - 1)
                await Task.Delay(1000);
        }
        
        return null;
    }

    private async Task<string?> TryGetVerificationCodeAsync(string email)
    {
        var response = await _httpClient.GetAsync($"{_mailhogBaseUrl}/api/v2/messages");
        response.EnsureSuccessStatusCode();
        
        var json = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var messages = JsonSerializer.Deserialize<MailHogResponse>(json, options);
        
        var latestEmail = messages?.Items
            ?.Where(m => m.Content?.Headers?.To?.Any(t => t.Contains(email)) == true)
            ?.OrderByDescending(m => m.Created)
            ?.FirstOrDefault();
            
        if (latestEmail?.Content?.Body == null) 
            return null;
        
        var match = Regex.Match(latestEmail.Content.Body, @"<div class=3D""otp-digits"">(\d{6})</div>");
        if (!match.Success)
        {
            match = Regex.Match(latestEmail.Content.Body, @"<div class=""otp-digits"">(\d{6})</div>");
        }
        
        return match.Success ? match.Groups[1].Value : null;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _httpClient.Dispose();
        }
    }
}

public class MailHogResponse
{
    public MailHogMessage[]? Items { get; set; }
    public int Total { get; set; }
    public int Count { get; set; }
}

public class MailHogMessage 
{
    public string? Id { get; set; }
    public MailHogFrom? From { get; set; }
    public MailHogContent? Content { get; set; }
    public string? Created { get; set; }
}

public class MailHogFrom
{
    public string? Mailbox { get; set; }
    public string? Domain { get; set; }
}

public class MailHogContent
{
    public MailHogHeaders? Headers { get; set; }
    public string? Body { get; set; }
}

public class MailHogHeaders
{
    public string[]? To { get; set; }
    public string[]? From { get; set; }
    public string[]? Subject { get; set; }
}