using System.Security.Claims;
using UniversitySocial.Api.Contracts;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Extensions;

public static class MappingExtensions
{
    public static Guid UserId(this ClaimsPrincipal principal) =>
        Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnauthorizedAccessException("Missing user identifier."));

    public static UserResponse ToResponse(this User user) => new(user.Id, user.Email, user.DisplayName,
        user.Role, user.Bio, user.Program, user.AcademicYear, user.IsActive, user.CreatedAt);

    public static PostResponse ToResponse(this Post post) => new(post.Id, post.AuthorId, post.Author.DisplayName,
        post.GroupId, post.Content, post.CreatedAt, post.UpdatedAt, post.Comments.Count, post.Reactions.Count);

    public static GroupResponse ToResponse(this Group group, Guid? viewerId = null) => new(group.Id, group.OwnerId,
        group.Owner.DisplayName, group.Name, group.Description, group.CreatedAt, group.Members.Count, group.Posts.Count,
        viewerId.HasValue && group.Members.Any(x => x.UserId == viewerId));

    public static MessageResponse ToResponse(this Message message) => new(message.Id, message.SenderId,
        message.Sender.DisplayName, message.RecipientId, message.Recipient.DisplayName, message.Body,
        message.CreatedAt, message.ReadAt);
}
