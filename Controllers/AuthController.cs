using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using CardDuel.ServerApi.Infrastructure;
using CardDuel.ServerApi.Infrastructure.Models;

namespace CardDuel.ServerApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    AppDbContext dbContext,
    IConfiguration configuration,
    ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Email, username, and password are required" });
        }

        if (await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email) != null)
        {
            return BadRequest(new { message = "Email already registered" });
        }

        if (await dbContext.Users.FirstOrDefaultAsync(u => u.Username == request.Username) != null)
        {
            return BadRequest(new { message = "Username already taken" });
        }

        var user = new UserAccount
        {
            Email = request.Email,
            Username = request.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        var rating = new PlayerRating { UserId = user.Id };
        dbContext.Ratings.Add(rating);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("User registered: {UserId} ({Email})", user.Id, user.Email);

        var token = GenerateJwtToken(user.Id, user.Email);
        return Ok(new AuthResponse(token, user.Id, user.Username, user.Email));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email and password are required" });
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        // Support both BCrypt and SHA256 hashes (for seeded test users)
        var passwordValid = false;
        try
        {
            passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Hash is not BCrypt, try SHA256
        }

        if (!passwordValid)
        {
            // Fallback: check SHA256 (for seeded test accounts)
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(request.Password));
                var sha256Hash = Convert.ToBase64String(hashedBytes);
                passwordValid = user.PasswordHash == sha256Hash;
            }
        }

        if (!passwordValid)
        {
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            return Unauthorized(new { message = "Invalid email or password" });
        }

        if (!user.IsActive)
        {
            return Unauthorized(new { message = "Account is deactivated" });
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        logger.LogInformation("User logged in: {UserId} ({Email})", user.Id, user.Email);

        var token = GenerateJwtToken(user.Id, user.Email);
        return Ok(new AuthResponse(token, user.Id, user.Username, user.Email));
    }

    private string GenerateJwtToken(string userId, string email)
    {
        var signingKey = configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("Missing Jwt:SigningKey");
        var issuer = configuration["Jwt:Issuer"] ?? "cardduel-server";
        var audience = configuration["Jwt:Audience"] ?? "cardduel-clients";

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("sub", userId),
            new Claim(ClaimTypes.Email, email),
            new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public sealed record RegisterRequest(
    [Required] string Email,
    [Required] string Username,
    [Required] string Password);

public sealed record LoginRequest(
    [Required] string Email,
    [Required] string Password);

public sealed record AuthResponse(
    string token,
    string userId,
    string username,
    string email);
