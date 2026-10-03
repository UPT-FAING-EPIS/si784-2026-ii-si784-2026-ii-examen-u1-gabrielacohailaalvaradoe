using System.ComponentModel.DataAnnotations;

namespace UniversitySocial.Api.Contracts;

public sealed record RegisterRequest(
    [Required, EmailAddress, MaxLength(254)] string Email,
    [Required, MinLength(10), MaxLength(128)] string Password,
    [Required, MinLength(2), MaxLength(100)] string DisplayName,
    [Required] string Role);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(string Token, UserResponse User);

public sealed record UpdateUserRequest(
    [Required, MinLength(2), MaxLength(100)] string DisplayName,
    [MaxLength(500)] string? Bio,
    [MaxLength(120)] string? Program,
    [MaxLength(40)] string? AcademicYear);

public sealed record AdminUpdateUserRequest(string? Role, bool? IsActive);

public sealed record UserResponse(Guid Id, string Email, string DisplayName, string Role, string? Bio,
    string? Program, string? AcademicYear, bool IsActive, DateTimeOffset CreatedAt);

public sealed record CreatePostRequest(
    [Required, MinLength(1), MaxLength(2000)] string Content,
    Guid? GroupId);
public sealed record UpdatePostRequest([Required, MinLength(1), MaxLength(2000)] string Content);
public sealed record PostResponse(Guid Id, Guid AuthorId, string AuthorName, Guid? GroupId, string Content,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt, int CommentCount, int ReactionCount);

public sealed record CreateCommentRequest(
    [Required] Guid PostId,
    [Required, MinLength(1), MaxLength(1000)] string Content);
public sealed record CommentResponse(Guid Id, Guid PostId, Guid AuthorId, string AuthorName, string Content, DateTimeOffset CreatedAt);
public sealed record ReactionRequest([Required] string Type);

public sealed record CreateGroupRequest(
    [Required, MinLength(2), MaxLength(120)] string Name,
    [Required, MaxLength(1000)] string Description);
public sealed record UpdateGroupRequest(
    [Required, MinLength(2), MaxLength(120)] string Name,
    [Required, MaxLength(1000)] string Description);
public sealed record GroupResponse(Guid Id, Guid OwnerId, string OwnerName, string Name, string Description,
    DateTimeOffset CreatedAt, int MemberCount, int PostCount, bool IsMember);

public sealed record CreateMessageRequest(
    [Required] Guid RecipientId,
    [Required, MinLength(1), MaxLength(2000)] string Body);
public sealed record MessageResponse(Guid Id, Guid SenderId, string SenderName, Guid RecipientId,
    string RecipientName, string Body, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total)
{
    public int TotalPages => (int)Math.Ceiling(Total / (double)PageSize);
}

public sealed record SearchResponse(
    PagedResponse<UserResponse> Users,
    PagedResponse<GroupResponse> Groups,
    PagedResponse<PostResponse> Posts);
