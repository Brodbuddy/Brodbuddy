using Application.Models;
using Core.Exceptions;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Exceptions;
using JWT.Serializers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public interface IJwtService
{
    string Generate(string subject, string email, string role);
    bool TryValidate(string jwt, out JwtClaims validatedClaims);
}

public class JwtService(IOptionsMonitor<AppOptions> optionsMonitor, TimeProvider timeProvider, ILogger<JwtService> logger) : IJwtService
{
    public string Generate(string subject, string email, string role)
    {
        if (string.IsNullOrEmpty(subject))
            throw new ArgumentException("Subject cannot be null or empty", nameof(subject));
    
        if (string.IsNullOrEmpty(email))
            throw new ArgumentException("Email cannot be null or empty", nameof(email));
    
        if (string.IsNullOrEmpty(role))
            throw new ArgumentException("Role cannot be null or empty", nameof(role));
    
        try
        {
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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate JWT token");
            throw new AuthenticationException("Failed to generate authentication token", "TokenGenerationFailed", ex);
        }
    }

    public bool TryValidate(string jwt, out JwtClaims validatedClaims)
    {
        validatedClaims = null!;
        
        if (string.IsNullOrEmpty(jwt))
        {
            throw new ArgumentException("JWT cannot be null or empty", nameof(jwt));
        }

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

            if (decoded.Iss != jwtOptions.Issuer || decoded.Aud != jwtOptions.Audience)
                throw new AuthenticationException("Invalid token issuer or audience", 
                    "TokenValidationFailed");

            validatedClaims = decoded;
            return true;
        }
        catch (TokenExpiredException ex)
        {
            logger.LogWarning(ex, "Token expired: {Message}", ex.Message);
            throw new AuthenticationException("Token has expired", "TokenExpired", ex);
        }
        catch (SignatureVerificationException ex)
        {
            logger.LogWarning(ex, "Signature verification failed: {Message}", ex.Message);
            throw new AuthenticationException("Token signature verification failed", "InvalidSignature", ex);
        }
        catch (AuthenticationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred: {Message}", ex.Message);
            throw new AuthenticationException("Token validation failed", "UnknownValidationError", ex);
        }
    }

    private sealed class TimeProviderAdapter(TimeProvider timeProvider) : IDateTimeProvider
    {
        private readonly TimeProvider _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        public DateTimeOffset GetNow() => _timeProvider.GetUtcNow();
    }
}