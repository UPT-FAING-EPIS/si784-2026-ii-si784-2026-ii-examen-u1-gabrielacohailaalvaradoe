using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;
using UniversitySocial.Api.Models;
using UniversitySocial.Api.Services;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("auth")]
public sealed class AuthController(AppDbContext db, ITokenService tokens) : ControllerBase
{
    private readonly PasswordHasher<User> _hasher = new();

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var role = request.Role.Trim().ToLowerInvariant();
        if (!Roles.Allowed.Contains(role) || role == Roles.Administrator)
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["role"] = ["Choose student, teacher, or staff."] }));
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(x => x.Email == email))
            return Conflict(new ProblemDetails { Status = 409, Title = "Email is already registered." });
        var user = new User { Email = email, DisplayName = request.DisplayName.Trim(), Role = role };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = user.Id }, new AuthResponse(tokens.Create(user), user.ToResponse()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email);
        if (user is null || !user.IsActive || _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new ProblemDetails { Status = 401, Title = "Invalid credentials." });
        return Ok(new AuthResponse(tokens.Create(user), user.ToResponse()));
    }
}
