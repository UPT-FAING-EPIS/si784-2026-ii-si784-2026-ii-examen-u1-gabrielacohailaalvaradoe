using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("posts"), Authorize]
public sealed class PostsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PostResponse>> Create(CreatePostRequest request)
    {
        var userId = User.UserId();
        if (request.GroupId.HasValue && !await db.GroupMembers.AnyAsync(x => x.GroupId == request.GroupId && x.UserId == userId))
            return Forbid();
        var post = new Post { AuthorId = userId, GroupId = request.GroupId, Content = request.Content.Trim() };
        db.Posts.Add(post);
        await db.SaveChangesAsync();
        await db.Entry(post).Reference(x => x.Author).LoadAsync();
        return CreatedAtAction(nameof(GetById), new { id = post.Id }, post.ToResponse());
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<PostResponse>>> GetAll([FromQuery] string? q, [FromQuery] Guid? groupId,
        [FromQuery] Guid? authorId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        (page, pageSize) = (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        var query = db.Posts.AsNoTracking().Include(x => x.Author).Include(x => x.Comments).Include(x => x.Reactions).AsQueryable();
        if (groupId.HasValue) query = query.Where(x => x.GroupId == groupId);
        if (authorId.HasValue) query = query.Where(x => x.AuthorId == authorId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(x => x.Content.ToLower().Contains(term));
        }
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResponse<PostResponse>(rows.Select(x => x.ToResponse()).ToList(), page, pageSize, total));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PostResponse>> GetById(Guid id)
    {
        var post = await db.Posts.AsNoTracking().Include(x => x.Author).Include(x => x.Comments).Include(x => x.Reactions).SingleOrDefaultAsync(x => x.Id == id);
        return post is null ? NotFound() : Ok(post.ToResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PostResponse>> Update(Guid id, UpdatePostRequest request)
    {
        var post = await db.Posts.Include(x => x.Author).Include(x => x.Comments).Include(x => x.Reactions).SingleOrDefaultAsync(x => x.Id == id);
        if (post is null) return NotFound();
        if (post.AuthorId != User.UserId() && !User.IsInRole(Roles.Administrator)) return Forbid();
        post.Content = request.Content.Trim();
        post.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return Ok(post.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var post = await db.Posts.FindAsync(id);
        if (post is null) return NotFound();
        if (post.AuthorId != User.UserId() && !User.IsInRole(Roles.Administrator)) return Forbid();
        db.Posts.Remove(post);
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id:guid}/reaction")]
    public async Task<IActionResult> React(Guid id, ReactionRequest request)
    {
        var allowed = new[] { "like", "celebrate", "support", "insightful" };
        var type = request.Type.Trim().ToLowerInvariant();
        if (!allowed.Contains(type)) return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["type"] = ["Unknown reaction."] }));
        if (!await db.Posts.AnyAsync(x => x.Id == id)) return NotFound();
        var userId = User.UserId();
        var reaction = await db.Reactions.SingleOrDefaultAsync(x => x.PostId == id && x.UserId == userId);
        if (reaction is null) db.Reactions.Add(new Reaction { PostId = id, UserId = userId, Type = type });
        else reaction.Type = type;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}/reaction")]
    public async Task<IActionResult> RemoveReaction(Guid id)
    {
        var reaction = await db.Reactions.SingleOrDefaultAsync(x => x.PostId == id && x.UserId == User.UserId());
        if (reaction is null) return NotFound();
        db.Reactions.Remove(reaction);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
