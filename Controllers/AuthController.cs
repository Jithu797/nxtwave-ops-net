using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LMSDashboard.DTOs;

namespace LMSDashboard.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config) => _config = config;

    public record LoginRequest(string Username, string Password);
    public record TokenResponse(string Token, DateTime Expires);

    /// <summary>Obtain a JWT token. For dev use only — replace with real identity in production.</summary>
    [HttpPost("token")]
    [ProducesResponseType(typeof(ApiResponse<TokenResponse>), 200)]
    [ProducesResponseType(401)]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        var validUser = _config["Auth:DevUsername"] ?? "admin";
        var validPass = _config["Auth:DevPassword"] ?? "admin123";

        if (request.Username != validUser || request.Password != validPass)
            return Unauthorized(ApiResponse<TokenResponse>.Fail("Invalid credentials."));

        var secret = _config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured.");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(8);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"] ?? "LMSDashboard",
            audience: _config["Jwt:Audience"] ?? "LMSDashboard",
            claims: new[]
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.Role, "ContentOps")
            },
            expires: expires,
            signingCredentials: creds);

        var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);
        return Ok(ApiResponse<TokenResponse>.Ok(new TokenResponse(tokenStr, expires)));
    }
}
