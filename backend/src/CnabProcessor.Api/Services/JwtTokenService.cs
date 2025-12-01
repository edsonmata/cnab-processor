// ========================================
// File: CnabProcessor.Api/Services/JwtTokenService.cs
// Purpose: JWT token generation and validation
// ========================================

using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CnabProcessor.Api.Services;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public class JwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenService> _logger;

    public JwtTokenService(IConfiguration configuration, ILogger<JwtTokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generates a JWT token for the specified username.
    /// </summary>
    /// <param name="username">Username to generate token for</param>
    /// <returns>Token string and expiration time</returns>
    public (string token, DateTime expiresAt) GenerateToken(string username)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = jwtSettings["Issuer"] ?? "CnabProcessor";
        var audience = jwtSettings["Audience"] ?? "CnabProcessorUsers";
        var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        _logger.LogInformation("Generated JWT token for user: {Username}, expires at: {ExpiresAt}",
            username, expiresAt);

        return (tokenString, expiresAt);
    }

    /// <summary>
    /// Validates credentials (simplified for demo - use proper user service in production).
    /// </summary>
    /// <param name="username">Username</param>
    /// <param name="password">Password</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool ValidateCredentials(string username, string password)
    {
        // IMPORTANT: This is a simplified demo implementation
        // In production, validate against a proper user database with hashed passwords

        var demoUsers = _configuration.GetSection("DemoUsers").Get<Dictionary<string, string>>()
            ?? new Dictionary<string, string>
            {
                { "admin", "Admin@123" },
                { "user", "User@123" }
            };

        if (demoUsers.TryGetValue(username, out var storedPassword))
        {
            // In production, use password hashing (BCrypt, Argon2, etc.)
            return password == storedPassword;
        }

        return false;
    }
}
