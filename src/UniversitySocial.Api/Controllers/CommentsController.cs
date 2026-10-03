using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("comments"), Authorize]
public sealed class CommentsController(AppDbContext db) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CommentResponse>> Create(CreateCommentRequest request)
    {
        if (!await db.Posts.AnyAsync(x => x.Id == request.PostId)) return NotFound(new ProblemDetails { Title = "Post not found." });
        var comment = new Comment { PostId = request.PostId, AuthorId = User.UserId(), Content = request.Content.Trim() };
        db.Comments.Add(comment);
        await db.SaveChangesAsync();
        await db.Entry(comment).Reference(x => x.Author).LoadAsync();
        var response = new CommentResponse(comment.Id, comment.PostId, comment.AuthorId, comment.Author.DisplayName, comment.Content, comment.CreatedAt);
        return Created($"/comments/{comment.Id}", response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var comment = await db.Comments.FindAsync(id);
        if (comment is null) return NotFound();
        if (comment.AuthorId != User.UserId() && !User.IsInRole(Roles.Administrator)) return Forbid();
        db.Comments.Remove(comment);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
