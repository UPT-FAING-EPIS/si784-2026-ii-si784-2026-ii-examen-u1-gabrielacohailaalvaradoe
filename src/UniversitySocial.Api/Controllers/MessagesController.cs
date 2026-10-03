using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("messages"), Authorize]
public sealed class MessagesController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<MessageResponse>> Create(CreateMessageRequest request)
    {
        var senderId = User.UserId();
        if (request.RecipientId == senderId) return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["recipientId"] = ["Cannot send a private message to yourself."] }));
        if (!await db.Users.AnyAsync(x => x.Id == request.RecipientId && x.IsActive)) return NotFound(new ProblemDetails { Title = "Recipient not found." });
        var message = new Message { SenderId = senderId, RecipientId = request.RecipientId, Body = request.Body.Trim() };
        db.Messages.Add(message);
        await db.SaveChangesAsync();
        await db.Entry(message).Reference(x => x.Sender).LoadAsync();
        await db.Entry(message).Reference(x => x.Recipient).LoadAsync();
        return Created($"/messages/{message.Id}", message.ToResponse());
    }

    [HttpGet("conversation/{otherUserId:guid}")]
    public async Task<ActionResult<PagedResponse<MessageResponse>>> Conversation(Guid otherUserId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        (page, pageSize) = (Math.Max(1, page), Math.Clamp(pageSize, 1, 100));
        var me = User.UserId();
        var query = db.Messages.AsNoTracking().Include(x => x.Sender).Include(x => x.Recipient)
            .Where(x => x.SenderId == me && x.RecipientId == otherUserId || x.SenderId == otherUserId && x.RecipientId == me);
        var total = await query.CountAsync();
        var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return Ok(new PagedResponse<MessageResponse>(rows.OrderBy(x => x.CreatedAt).Select(x => x.ToResponse()).ToList(), page, pageSize, total));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MessageResponse>>> Inbox()
    {
        var me = User.UserId();
        var rows = await db.Messages.AsNoTracking().Include(x => x.Sender).Include(x => x.Recipient)
            .Where(x => x.SenderId == me || x.RecipientId == me).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync();
        return Ok(rows.Select(x => x.ToResponse()).ToList());
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var message = await db.Messages.FindAsync(id);
        if (message is null) return NotFound();
        if (message.RecipientId != User.UserId()) return Forbid();
        message.ReadAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }
}
