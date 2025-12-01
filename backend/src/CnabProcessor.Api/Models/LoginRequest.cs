// ========================================
// File: CnabProcessor.Api/Models/LoginRequest.cs
// Purpose: Login request model
// ========================================

using System.ComponentModel.DataAnnotations;

namespace CnabProcessor.Api.Models;

/// <summary>
/// Request model for user authentication.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Username for authentication.
    /// </summary>
    [Required(ErrorMessage = "Username is required")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Password for authentication.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Response model for successful authentication.
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in ISO 8601 format.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Username of authenticated user.
    /// </summary>
    public string Username { get; set; } = string.Empty;
}
