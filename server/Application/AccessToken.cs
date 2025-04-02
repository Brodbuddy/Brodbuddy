using Application.Models;
using JWT;
using JWT.Algorithms;
using JWT.Builder;
using JWT.Serializers;
using Microsoft.Extensions.Options;


namespace Application;


public interface  IAccessToken
{
    string Generate(JwtClaims claims);
    JwtClaims Validate(string jwt);
}


public class AccessToken(IOptionsMonitor<AppOptions> optionsMonitor, TimeProvider timeProvider) : IAccessToken
{
    
    
    public string Generate(JwtClaims claims)
    {
        if (string.IsNullOrEmpty(claims.Exp))
        {
            var currentTime = timeProvider.GetUtcNow(); 
            var expirationTime = currentTime.AddMinutes(15); 
            claims.Exp = expirationTime.ToUnixTimeSeconds().ToString();
        }    
        

        var tokenBuilder = new JwtBuilder()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(optionsMonitor.CurrentValue.JwtSecret)
            .WithUrlEncoder(new JwtBase64UrlEncoder())
            .WithJsonSerializer(new JsonNetSerializer())
            .AddHeader(HeaderName.Type, "JWT");

        foreach (var claim in claims.GetType().GetProperties())
            tokenBuilder.AddClaim(claim.Name, claim.GetValue(claims)!.ToString());
        return tokenBuilder.Encode();
    }

    public JwtClaims Validate(string jwt)
    {
        var token = new JwtBuilder()
            .WithAlgorithm(new HMACSHA512Algorithm())
            .WithSecret(optionsMonitor.CurrentValue.JwtSecret)
            .WithUrlEncoder(new JwtBase64UrlEncoder())
            .WithJsonSerializer(new JsonNetSerializer())
            .MustVerifySignature()
            .Decode<JwtClaims>(jwt);

        return token;
    }
}