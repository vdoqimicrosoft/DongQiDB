using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using DongQiDB.Api.DTOs.Auth;
using DongQiDB.Application.DTOs;

namespace DongQiDB.Api.Controllers;

/// <summary>
/// Authentication controller
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[ApiVersion("1")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Login with username and password
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status401Unauthorized)]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // TODO: Implement actual authentication logic
        // For now, use a simple validation for demo purposes
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return Unauthorized(Result<LoginResponse>.Fail(
                Domain.Common.ErrorCode.Unauthorized,
                "Username and password are required"));
        }

        // Demo: Accept any non-empty credentials
        // In production, validate against user store
        var userId = Guid.NewGuid().ToString();
        var token = GenerateJwtToken(userId, request.Username);
        var refreshToken = GenerateRefreshToken();

        _logger.LogInformation("User {Username} logged in successfully", request.Username);

        return Ok(Result<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            TokenType = "Bearer"
        }));
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<LoginResponse>), StatusCodes.Status401Unauthorized)]
    public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
    {
        // TODO: Implement actual refresh token validation
        // In production, validate refresh token against store
        if (string.IsNullOrEmpty(request.RefreshToken))
        {
            return Unauthorized(Result<LoginResponse>.Fail(
                Domain.Common.ErrorCode.Unauthorized,
                "Refresh token is required"));
        }

        // Demo: Generate new tokens
        var userId = Guid.NewGuid().ToString();
        var username = "user"; // Would come from refresh token in production
        var token = GenerateJwtToken(userId, username);
        var refreshToken = GenerateRefreshToken();

        _logger.LogInformation("Token refreshed for user {Username}", username);

        return Ok(Result<LoginResponse>.Ok(new LoginResponse
        {
            AccessToken = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            TokenType = "Bearer"
        }));
    }

    /// <summary>
    /// Validate current token
    /// </summary>
    [HttpGet("validate")]
    [Authorize]
    [ProducesResponseType(typeof(Result<TokenValidationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Result<TokenValidationResponse>), StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(ClaimTypes.Name)?.Value;
        var expiresAt = User.FindFirst("exp")?.Value;

        return Ok(Result<TokenValidationResponse>.Ok(new TokenValidationResponse
        {
            IsValid = true,
            UserId = userId,
            Username = username,
            ExpiresAt = expiresAt != null
                ? DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiresAt)).UtcDateTime
                : DateTime.UtcNow.AddHours(1)
        }));
    }

    private string GenerateJwtToken(string userId, string username)
    {
        var secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT secret key is not configured");
        var issuer = _configuration["Jwt:Issuer"] ?? "DongQiDB";
        var audience = _configuration["Jwt:Audience"] ?? "DongQiDB";
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            + Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
}
