namespace UniversitySocial.Api.Models;

public static class Roles
{
    public const string Student = "student";
    public const string Teacher = "teacher";
    public const string Staff = "staff";
    public const string Administrator = "administrator";
    public const string All = Student + "," + Teacher + "," + Staff + "," + Administrator;
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
        { Student, Teacher, Staff, Administrator };
}

public sealed class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Role { get; set; } = Roles.Student;
    public string? Bio { get; set; }
    public string? Program { get; set; }
    public string? AcademicYear { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<Post> Posts { get; set; } = [];
    public ICollection<GroupMember> GroupMemberships { get; set; } = [];
}

public sealed class Post
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public ICollection<Comment> Comments { get; set; } = [];
    public ICollection<Reaction> Reactions { get; set; } = [];
}

public sealed class Comment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = null!;
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Reaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PostId { get; set; }
    public Post Post { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string Type { get; set; } = "like";
}

public sealed class Group
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<GroupMember> Members { get; set; } = [];
    public ICollection<Post> Posts { get; set; } = [];
}

public sealed class GroupMember
{
    public Guid GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string MembershipRole { get; set; } = "member";
    public DateTimeOffset JoinedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Message
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SenderId { get; set; }
    public User Sender { get; set; } = null!;
    public Guid RecipientId { get; set; }
    public User Recipient { get; set; } = null!;
    public string Body { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReadAt { get; set; }
}
