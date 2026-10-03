using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("dashboard"), Authorize]
public sealed class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Personal()
    {
        var id = User.UserId();
        var user = await db.Users.AsNoTracking().SingleAsync(x => x.Id == id);
        return Ok(new
        {
            profile = user.ToResponse(),
            postCount = await db.Posts.CountAsync(x => x.AuthorId == id),
            groupCount = await db.GroupMembers.CountAsync(x => x.UserId == id),
            unreadMessages = await db.Messages.CountAsync(x => x.RecipientId == id && x.ReadAt == null)
        });
    }

    [HttpGet("admin"), Authorize(Roles = Roles.Administrator)]
    public async Task<IActionResult> Administration() => Ok(new
    {
        users = await db.Users.CountAsync(), activeUsers = await db.Users.CountAsync(x => x.IsActive),
        groups = await db.Groups.CountAsync(), posts = await db.Posts.CountAsync(), comments = await db.Comments.CountAsync(),
        messages = await db.Messages.CountAsync()
    });
}
