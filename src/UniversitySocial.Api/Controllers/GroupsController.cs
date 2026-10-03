using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("groups"), Authorize]
public sealed class GroupsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<GroupResponse>> Create(CreateGroupRequest request)
    {
        var userId = User.UserId();
        var group = new Group { OwnerId = userId, Name = request.Name.Trim(), Description = request.Description.Trim() };
        group.Members.Add(new GroupMember { Group = group, UserId = userId, MembershipRole = "owner" });
        db.Groups.Add(group);
        await db.SaveChangesAsync();
        await db.Entry(group).Reference(x => x.Owner).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = group.Id }, group.ToResponse(userId));
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<GroupResponse>>> GetAll([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        (page, pageSize) = (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        var query = db.Groups.AsNoTracking().Include(x => x.Owner).Include(x => x.Members).Include(x => x.Posts).AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Name.ToLower().Contains(term) || x.Description.ToLower().Contains(term));
        }
        var total = await query.CountAsync();
        var rows = await query.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var viewer = User.UserId();
        return Ok(new PagedResponse<GroupResponse>(rows.Select(x => x.ToResponse(viewer)).ToList(), page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GroupResponse>> GetById(Guid id)
    {
        var group = await db.Groups.AsNoTracking().Include(x => x.Owner).Include(x => x.Members).Include(x => x.Posts).SingleOrDefaultAsync(x => x.Id == id);
        return group is null ? NotFound() : Ok(group.ToResponse(User.UserId()));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<GroupResponse>> Update(Guid id, UpdateGroupRequest request)
    {
        var group = await db.Groups.Include(x => x.Owner).Include(x => x.Members).Include(x => x.Posts).SingleOrDefaultAsync(x => x.Id == id);
        if (group is null) return NotFound();
        if (group.OwnerId != User.UserId() && !User.IsInRole(Roles.Administrator)) return Forbid();
        group.Name = request.Name.Trim();
        group.Description = request.Description.Trim();
        await db.SaveChangesAsync();
        return Ok(group.ToResponse(User.UserId()));
    }

    [HttpPost("{id:guid}/members")]
    public async Task<IActionResult> Join(Guid id)
    {
        if (!await db.Groups.AnyAsync(x => x.Id == id)) return NotFound();
        var userId = User.UserId();
        if (!await db.GroupMembers.AnyAsync(x => x.GroupId == id && x.UserId == userId))
        {
            db.GroupMembers.Add(new GroupMember { GroupId = id, UserId = userId });
            await db.SaveChangesAsync();
        }
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/me")]
    public async Task<IActionResult> Leave(Guid id)
    {
        var userId = User.UserId();
        var group = await db.Groups.FindAsync(id);
        if (group is null) return NotFound();
        if (group.OwnerId == userId) return Conflict(new ProblemDetails { Status = 409, Title = "The owner cannot leave the group." });
        var membership = await db.GroupMembers.FindAsync(id, userId);
        if (membership is null) return NotFound();
        db.GroupMembers.Remove(membership);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var group = await db.Groups.FindAsync(id);
        if (group is null) return NotFound();
        if (group.OwnerId != User.UserId() && !User.IsInRole(Roles.Administrator)) return Forbid();
        db.Groups.Remove(group);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
