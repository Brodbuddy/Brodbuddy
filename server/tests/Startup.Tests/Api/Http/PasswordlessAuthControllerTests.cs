using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Api.Http.Models;
using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using SharedTestDependencies.Constants;
using Startup.Tests.Infrastructure.Bases;
using Startup.Tests.Infrastructure.Extensions;
using Startup.Tests.Infrastructure.Fixtures;
using Xunit.Abstractions;

namespace Startup.Tests.Api.Http;

[Collection(TestCollections.Startup)]
public partial class PasswordlessAuthControllerTests(StartupTestFixture fixture, ITestOutputHelper output) : ApiTestBase(fixture, output)
{
    private static string GetUniqueEmail() => $"test-{Guid.NewGuid()}@example.com";

    [Fact]
    public async Task InitiateLogin_ValidEmail_ShouldReturn200()
    {
        string uniqueEmail = GetUniqueEmail();
        
        // Arrange
        var client = Factory.CreateClient();
        var request = new InitiateLoginRequest(uniqueEmail);
        
        // Act
        var response = await client.PostAsJsonAsync(Routes.PasswordlessAuth.Initiate, request);
            
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        
        await this.WhenReadyAsync(async void () =>
        {
            await WithDbContextAsync(async dbContext =>
            {
                var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == uniqueEmail);
                user.ShouldNotBeNull();
                
                var verificationContext = await dbContext.VerificationContexts
                    .Include(vc => vc.Otp)
                    .FirstOrDefaultAsync(vc => vc.UserId == user.Id);
                
                verificationContext.ShouldNotBeNull();
                verificationContext.Otp.ShouldNotBeNull();
                verificationContext.Otp.IsUsed.ShouldBeFalse();
            });
        });
    }

    [Fact]
    public async Task InitiateLogin_InvalidEmail_ShouldReturn400()
    {
        // Arrange
        var client = Factory.CreateClient();
        var request = new InitiateLoginRequest("not-an-email");
        
        // Act
        var response = await client.PostAsJsonAsync(Routes.PasswordlessAuth.Initiate, request);
            
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task VerifyCode_WithValidCode_ShouldReturnAccessToken()
    {
        // Arrange
        var emailSender = GetEmailSender();
        emailSender.ClearEmails(); 
        
        var client = Factory.CreateClient();
        var email = GetUniqueEmail();
        
        // Initiate login for opret bruger og OTP 
        await client.PostAsJsonAsync(Routes.PasswordlessAuth.Initiate, new InitiateLoginRequest(email));
        
        var otpCode = 0;
        await this.WhenReadyAsync(() =>
        {
            var code = emailSender.GetLatestOtpFor(email);
            code.ShouldNotBeNull($"No OTP email was sent to {email}");
            otpCode = code.Value;
        });
        
        
        var user = await WithDbContextAsync(async dbContext => await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email));
        user.ShouldNotBeNull();
        
        await WithDbContextAsync(async dbContext =>
        {
            var verificationContext = await dbContext.VerificationContexts
                .Include(vc => vc.Otp)
                .FirstOrDefaultAsync(vc => vc.UserId == user.Id);
            
            verificationContext.ShouldNotBeNull();
            verificationContext.Otp.ShouldNotBeNull();
            verificationContext.Otp.IsUsed.ShouldBeFalse("OTP should not be marked as used before verification");
            verificationContext.Otp.Code.ShouldBe(otpCode, "OTP code in database should match the one sent via email");
        });
        
        // Act
        var verifyResponse = await client.PostAsJsonAsync(Routes.PasswordlessAuth.Verify, new LoginVerificationRequest(email, otpCode));
        
        // Assert
        verifyResponse.IsSuccessStatusCode.ShouldBeTrue();
        var result = await verifyResponse.Content.ReadFromJsonAsync<LoginVerificationResponse>();
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNullOrEmpty();
        
        // Tjek for refreshToken cookie
        verifyResponse.Headers.TryGetValues("Set-Cookie", out var cookies);
        if (cookies != null)
        {
            var enumerable = cookies as string[] ?? cookies.ToArray();
            enumerable.ShouldNotBeNull();
            enumerable.Any(c => c.Contains("refreshToken")).ShouldBeTrue();
        }

        // Verificer OTP brugt
        await WithDbContextAsync(async dbContext =>
        {
            var verificationContext = await dbContext.VerificationContexts
                .Include(vc => vc.Otp)
                .FirstOrDefaultAsync(vc => vc.UserId == user.Id);
            
            verificationContext.ShouldNotBeNull();
            verificationContext.Otp.ShouldNotBeNull();
            verificationContext.Otp.IsUsed.ShouldBeTrue("OTP should be marked as used after verification");
        });
    }
    
    [Fact]
    public async Task VerifyCode_WithInvalidCode_ShouldReturnError()
    {
        // Arrange
        var emailSender = GetEmailSender();
        emailSender.ClearEmails();
        
        var client = Factory.CreateClient();
        var email = GetUniqueEmail();
        
        await client.PostAsJsonAsync(Routes.PasswordlessAuth.Initiate, new InitiateLoginRequest(email));
        
        await this.WhenReadyAsync(() =>
        {
            var code = emailSender.GetLatestOtpFor(email);
            code.ShouldNotBeNull($"No OTP email was sent to {email}");
        });
        
        // Act 
        var invalidCode = 999999; 
        var verifyResponse = await client.PostAsJsonAsync(Routes.PasswordlessAuth.Verify, new LoginVerificationRequest(email, invalidCode));
        
        // Assert
        verifyResponse.IsSuccessStatusCode.ShouldBeFalse("The request should fail with an invalid code");
        
        // Verificer OTP ikke brugt
        await WithDbContextAsync(async dbContext =>
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            user.ShouldNotBeNull();
            
            var verificationContext = await dbContext.VerificationContexts.Include(vc => vc.Otp)
                                                                          .FirstOrDefaultAsync(vc => vc.UserId == user.Id);
            
            verificationContext.ShouldNotBeNull();
            verificationContext.Otp.ShouldNotBeNull();
            verificationContext.Otp.IsUsed.ShouldBeFalse("OTP should not be marked as used after failed verification");
        });
    }
    
    [Fact]
    public async Task VerifyCode_WithNonexistentEmail_ShouldReturnError()
    {
        // Arrange
        var client = Factory.CreateClient();
        var email = GetUniqueEmail(); 
        
        // Act
        var verifyResponse = await client.PostAsJsonAsync(Routes.PasswordlessAuth.Verify, new LoginVerificationRequest(email, 123456));
        
        // Assert
        verifyResponse.IsSuccessStatusCode.ShouldBeFalse("The request should fail with a nonexistent email");
    }
    
    [Fact]
    public async Task UserInfo_WithValidToken_ShouldReturnUserData()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var email = GetUniqueEmail();
        var client = Factory.CreateAuthenticatedHttpClient(userId, email, "user");
        
        await WithDbContextAsync(async dbContext =>
        {
            var user = new User
            {
                Id = Guid.Parse(userId),
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            
            await dbContext.Users.AddAsync(user);
            await dbContext.SaveChangesAsync();
        });
        
        // Act
        var response = await client.GetFromJsonAsync<UserInfoResponse>(Routes.PasswordlessAuth.UserInfo);
        
        // Assert
        response.ShouldNotBeNull();
        response.Email.ShouldBe(email);
        response.IsAdmin.ShouldBeFalse();
    }
    
    [Fact]
    public async Task UserInfo_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var client = Factory.CreateClient();
        
        // Act
        var response = await client.GetAsync(Routes.PasswordlessAuth.UserInfo);
        
        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Logout_ShouldClearRefreshTokenCookie()
    {
        // Arrange
        var client = Factory.CreateAuthenticatedHttpClient();
        
        // Act
        var response = await client.PostAsync(Routes.PasswordlessAuth.Logout, null);
        
        // Assert
        response.IsSuccessStatusCode.ShouldBeTrue();
        
        response.Headers.TryGetValues("Set-Cookie", out var cookies);
        if (cookies != null)
        {
            var enumerable = cookies as string[] ?? cookies.ToArray();
            enumerable.ShouldNotBeNull();
        
            var refreshTokenCookie = enumerable.FirstOrDefault(c => c.Contains("refreshToken"));
            refreshTokenCookie.ShouldNotBeNull();
            refreshTokenCookie.ShouldContain("Expires=");
            refreshTokenCookie.ShouldContain("HttpOnly");
            refreshTokenCookie.ShouldContain("Secure");
        }
    }
    
    [Fact]
    public async Task RefreshToken_WithValidRefreshToken_ShouldReturnNewTokens()
    {
        // Arrange
        var emailSender = GetEmailSender();
        emailSender.ClearEmails(); 
        
        var client = Factory.CreateClient();
        var email = GetUniqueEmail();
       
        // Initiate login
        await client.PostAsJsonAsync(Routes.PasswordlessAuth.Initiate, new InitiateLoginRequest(email));
        
        var user = await WithDbContextAsync(async dbContext => await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email));
        user.ShouldNotBeNull();
        
        var otpCode = 0;
        await this.WhenReadyAsync(() =>
        {
            var code = emailSender.GetLatestOtpFor(email);
            code.ShouldNotBeNull($"No OTP email was sent to {email}");
            otpCode = code.Value;
        });
        
        // Verificer loginc
        var verifyResponse = await client.PostAsJsonAsync(Routes.PasswordlessAuth.Verify, 
            new LoginVerificationRequest(email, otpCode));
        
        verifyResponse.IsSuccessStatusCode.ShouldBeTrue();
        
        // Få refresh token cookie
        verifyResponse.Headers.TryGetValues("Set-Cookie", out var cookies);
        if (cookies == null) throw new InvalidOperationException();
        var enumerable = cookies as string[] ?? cookies.ToArray();
        enumerable.ShouldNotBeNull();
        var refreshTokenCookie = enumerable.First(c => c.Contains("refreshToken"));
        var refreshTokenMatch = MyRegex().Match(refreshTokenCookie);
        refreshTokenMatch.Success.ShouldBeTrue();
        var refreshTokenValue = refreshTokenMatch.Groups[1].Value;
        
        // Få refresh token ID fra database for verificering senere
        var refreshTokenInfo = await WithDbContextAsync(async dbContext => {
            var context = await dbContext.TokenContexts.Include(tc => tc.RefreshToken)
                                                       .FirstOrDefaultAsync(tc => tc.UserId == user.Id);
                
            context.ShouldNotBeNull("No token context found for user");
            return new { Id = context.RefreshTokenId, context.RefreshToken.Token };
        });
        
        // Lav refresh request med token
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, Routes.PasswordlessAuth.Refresh);
        refreshRequest.Headers.Add("Cookie", $"refreshToken={refreshTokenValue}");
        
        // Act
        var refreshResponse = await client.SendAsync(refreshRequest);
        
        // Assert
        refreshResponse.IsSuccessStatusCode.ShouldBeTrue();
        
        // Respons skal indeholde access token 
        var result = await refreshResponse.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        result.ShouldNotBeNull();
        result.AccessToken.ShouldNotBeNullOrEmpty();
        
        // Skal også have ny refresh token
        refreshResponse.Headers.TryGetValues("Set-Cookie", out var newCookies);
        if (newCookies == null) throw new InvalidOperationException();
        var values = newCookies as string[] ?? newCookies.ToArray();
        values.ShouldNotBeNull();
        var newRefreshCookie = values.FirstOrDefault(c => c.Contains("refreshToken"));
        newRefreshCookie.ShouldNotBeNull("No refresh token cookie found in response");
        newRefreshCookie.ShouldContain("HttpOnly");
        newRefreshCookie.ShouldContain("Secure");
        
        // Original token skal være erstattet og revoked
        await this.WhenReadyAsync(async void () =>
        {
            await WithDbContextAsync(async dbContext =>
            {
                var originalToken = await dbContext.RefreshTokens.FindAsync(refreshTokenInfo.Id);
                originalToken.ShouldNotBeNull("Original refresh token not found in database");
                originalToken.RevokedAt.ShouldNotBeNull("Original token should be revoked");
                originalToken.ReplacedByTokenId.ShouldNotBeNull("Original token should have replacedByTokenId");
            });
        });
    }
    
    [Fact]
    public async Task RefreshToken_WithInvalidRefreshToken_ShouldReturnError()
    {
        // Arrange
        var client = Factory.CreateClient();
        
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, Routes.PasswordlessAuth.Refresh);
        refreshRequest.Headers.Add("Cookie", "refreshToken=invalid-token-that-does-not-exist");
        
        // Act
        var response = await client.SendAsync(refreshRequest);
        
        // Assert
        response.IsSuccessStatusCode.ShouldBeFalse("The request should fail with an invalid token");
    }
    
    [Fact]
    public async Task RefreshToken_WithNoRefreshToken_ShouldReturnError()
    {
        // Arrange
        var client = Factory.CreateClient();
        
        // Act
        var response = await client.PostAsync(Routes.PasswordlessAuth.Refresh, null);
        
        // Assert
        response.IsSuccessStatusCode.ShouldBeFalse("The request should fail without a refresh token");
    }

    [GeneratedRegex("refreshToken=([^;]+)")]
    private static partial Regex MyRegex();
}