using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Data;
using UniversitySocial.Api.Extensions;

namespace UniversitySocial.Api.Controllers;

[ApiController, Route("search"), Authorize]
public sealed class SearchController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SearchResponse>> Search([FromQuery] string q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (string.IsNullOrWhiteSpace(q)) return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["q"] = ["Search text is required."] }));
        (page, pageSize) = (Math.Max(1, page), Math.Clamp(pageSize, 1, 50));
        var term = q.Trim().ToLower();
        var usersQuery = db.Users.AsNoTracking().Where(x => x.IsActive &&
            (x.DisplayName.ToLower().Contains(term) || x.Program != null && x.Program.ToLower().Contains(term)));
        var groupsQuery = db.Groups.AsNoTracking().Include(x => x.Owner).Include(x => x.Members).Include(x => x.Posts)
            .Where(x => x.Name.ToLower().Contains(term) || x.Description.ToLower().Contains(term));
        var postsQuery = db.Posts.AsNoTracking().Include(x => x.Author).Include(x => x.Comments).Include(x => x.Reactions)
            .Where(x => x.Content.ToLower().Contains(term));
        var usersTotal = await usersQuery.CountAsync();
        var groupsTotal = await groupsQuery.CountAsync();
        var postsTotal = await postsQuery.CountAsync();
        var users = await usersQuery.OrderBy(x => x.DisplayName).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var groups = await groupsQuery.OrderBy(x => x.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var posts = await postsQuery.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        var viewer = User.UserId();
        return Ok(new SearchResponse(
            new PagedResponse<UserResponse>(users.Select(x => x.ToResponse()).ToList(), page, pageSize, usersTotal),
            new PagedResponse<GroupResponse>(groups.Select(x => x.ToResponse(viewer)).ToList(), page, pageSize, groupsTotal),
            new PagedResponse<PostResponse>(posts.Select(x => x.ToResponse()).ToList(), page, pageSize, postsTotal)));
    }
}
