using Core.ValueObjects;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Serializers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Auth;

public interface IJwtService
{
    string Generate(string subject, string email, string role);
    bool TryValidate(string jwt, out JwtClaims validatedClaims);
}

public class JwtService(IOptionsMonitor<AppOptions> optionsMonitor, TimeProvider timeProvider, ILogger<JwtService> logger) : IJwtService
{
    public string Generate(string subject, string email, string role)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
        
        
        var jwtOptions = optionsMonitor.CurrentValue.Jwt;
        var now = timeProvider.GetUtcNow();

        var tokenBuilder = new JwtBuilder()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(jwtOptions.Secret)
            .WithUrlEncoder(new JwtBase64UrlEncoder())
            .WithJsonSerializer(new JsonNetSerializer())
            .AddClaim("iss", jwtOptions.Issuer)
            .AddClaim("aud", jwtOptions.Audience)
            .AddClaim("iat", now.ToUnixTimeSeconds())
            .AddClaim("exp", now.AddMinutes(jwtOptions.ExpirationMinutes).ToUnixTimeSeconds())
            .AddClaim("jti", Guid.NewGuid().ToString())
            .AddClaim("sub", subject)
            .AddClaim("email", email)
            .AddClaim("role", role)
            .AddHeader(HeaderName.Type, "JWT");

        return tokenBuilder.Encode();
    }

    public bool TryValidate(string jwt, out JwtClaims validatedClaims)
    {
        validatedClaims = null!;

        try
        {
            var jwtOptions = optionsMonitor.CurrentValue.Jwt;
            var timeAdapter = new TimeProviderAdapter(timeProvider);

            var decoded = new JwtBuilder()
                .WithAlgorithm(new HMACSHA512Algorithm())
                .WithSecret(optionsMonitor.CurrentValue.Jwt.Secret)
                .WithUrlEncoder(new JwtBase64UrlEncoder())
                .WithJsonSerializer(new JsonNetSerializer())
                .WithDateTimeProvider(timeAdapter)
                .MustVerifySignature()
                .Decode<JwtClaims>(jwt);

            if (decoded.Iss != jwtOptions.Issuer)
            {
                return false;
            }

            if (decoded.Aud != jwtOptions.Audience)
            {
                return false;
            }

            validatedClaims = decoded;
            return true;
        }
        catch (TokenExpiredException ex)
        {
            logger.LogWarning(ex, "Token expired: {Message}", ex.Message);

            return false;
        }
        catch (SignatureVerificationException ex)
        {
            logger.LogWarning(ex, "Signature verification failed: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            return false;
        }
    }

    private sealed class TimeProviderAdapter(TimeProvider timeProvider) : IDateTimeProvider
    {
        private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public DateTimeOffset GetNow() => _timeProvider.GetUtcNow();
    }
}