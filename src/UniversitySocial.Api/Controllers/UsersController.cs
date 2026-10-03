using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("users"), Authorize]
public sealed class UsersController(AppDbContext db) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetById(Guid id)
    {
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.IsActive);
        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserResponse>>> GetAll([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        (page, pageSize) = Normalize(page, pageSize);
        var query = db.Users.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.DisplayName.ToLower().Contains(term) || x.Program != null && x.Program.ToLower().Contains(term));
        }
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.DisplayName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResponse<UserResponse>(rows.Select(x => x.ToResponse()).ToList(), page, pageSize, total));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, UpdateUserRequest request)
    {
        if (User.UserId() != id && !User.IsInRole(Roles.Administrator)) return Forbid();
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        user.DisplayName = request.DisplayName.Trim();
        user.Bio = request.Bio?.Trim();
        user.Program = request.Program?.Trim();
        user.AcademicYear = request.AcademicYear?.Trim();
        await db.SaveChangesAsync();
        return Ok(user.ToResponse());
    }

    [HttpPatch("{id:guid}/administration"), Authorize(Roles = Roles.Administrator)]
    public async Task<ActionResult<UserResponse>> AdminUpdate(Guid id, AdminUpdateUserRequest request)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (request.Role is not null)
        {
            var role = request.Role.Trim().ToLowerInvariant();
            if (!Roles.Allowed.Contains(role)) return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["role"] = ["Unknown role."] }));
            user.Role = role;
        }
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        await db.SaveChangesAsync();
        return Ok(user.ToResponse());
    }

    private static (int Page, int Size) Normalize(int page, int size) => (Math.Max(1, page), Math.Clamp(size, 1, 100));
}
