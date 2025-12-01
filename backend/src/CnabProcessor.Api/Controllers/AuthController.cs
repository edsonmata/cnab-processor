// ========================================
// File: CnabProcessor.Api/Controllers/AuthController.cs
// Purpose: Authentication endpoints
// ========================================

using CnabProcessor.Api.Models;
using CnabProcessor.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CnabProcessor.Api.Controllers;

/// <summary>
/// Controller for authentication operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(JwtTokenService tokenService, ILogger<AuthController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Authenticates user and returns JWT token.
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token if successful</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { message = "Invalid request", errors = ModelState });
        }

        _logger.LogInformation("Login attempt for user: {Username}", request.Username);

        // Validate credentials
        if (!_tokenService.ValidateCredentials(request.Username, request.Password))
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Generate token
        var (token, expiresAt) = _tokenService.GenerateToken(request.Username);

        _logger.LogInformation("Successful login for user: {Username}", request.Username);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Username = request.Username
        });
    }

    /// <summary>
    /// Gets information about the currently authenticated user.
    /// </summary>
    /// <returns>User information</returns>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(200)]
    [ProducesResponseType(401)]
    public IActionResult GetCurrentUser()
    {
        var username = User.Identity?.Name;

        return Ok(new
        {
            username,
            authenticated = true,
            claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    /// <summary>
    /// Returns demo credentials for testing (remove in production).
    /// </summary>
    [HttpGet("demo-credentials")]
    [AllowAnonymous]
    [ProducesResponseType(200)]
    public IActionResult GetDemoCredentials()
    {
        return Ok(new
        {
            message = "Demo credentials for testing",
            users = new[]
            {
                new { username = "admin", password = "Admin@123", role = "Administrator" },
                new { username = "user", password = "User@123", role = "User" }
            },
            note = "REMOVE THIS ENDPOINT IN PRODUCTION!"
        });
    }
}
