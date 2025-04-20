using System.Net.Mail;

namespace Core.Validation;

public static class ValidationRules
{
    private const int OtpCodeLength = 6;

    public static bool IsValidOtpCodeFormat(int code)
    {
        var minimum = (int)Math.Pow(10, OtpCodeLength - 1);
        var maximum = (int)Math.Pow(10, OtpCodeLength) - 1;
        return code >= minimum && code <= maximum;
    }
    
    public static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        if (!MailAddress.TryCreate(email, out _)) return false;
        if (!email.Contains('@') || !email.Contains('.')) return false;

        var parts = email.Split('@');
        if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1])) return false;
    
        return parts[1].Contains('.');
    }
}