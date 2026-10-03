using Microsoft.EntityFrameworkCore;
using UniversitySocial.Api.Models;

namespace UniversitySocial.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Reaction> Reactions => Set<Reaction>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Email).HasMaxLength(254);
            entity.Property(x => x.DisplayName).HasMaxLength(100);
            entity.Property(x => x.Role).HasMaxLength(32);
        });
        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasIndex(x => new { x.CreatedAt, x.Id });
            entity.Property(x => x.Content).HasMaxLength(2000);
            entity.HasOne(x => x.Author).WithMany(x => x.Posts).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Group).WithMany(x => x.Posts).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.Property(x => x.Content).HasMaxLength(1000);
            entity.HasOne(x => x.Post).WithMany(x => x.Comments).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Reaction>(entity =>
        {
            entity.HasIndex(x => new { x.PostId, x.UserId }).IsUnique();
            entity.Property(x => x.Type).HasMaxLength(20);
            entity.HasOne(x => x.Post).WithMany(x => x.Reactions).HasForeignKey(x => x.PostId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Group>(entity =>
        {
            entity.HasIndex(x => x.Name);
            entity.Property(x => x.Name).HasMaxLength(120);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasKey(x => new { x.GroupId, x.UserId });
            entity.HasOne(x => x.Group).WithMany(x => x.Members).HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.User).WithMany(x => x.GroupMemberships).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasIndex(x => new { x.SenderId, x.RecipientId, x.CreatedAt });
            entity.Property(x => x.Body).HasMaxLength(2000);
            entity.HasOne(x => x.Sender).WithMany().HasForeignKey(x => x.SenderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Recipient).WithMany().HasForeignKey(x => x.RecipientId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
