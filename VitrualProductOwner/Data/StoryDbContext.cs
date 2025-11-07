using BlazorApp1.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorApp1.Data;

public class StoryDbContext : DbContext
{
    public StoryDbContext(DbContextOptions<StoryDbContext> options) : base(options)
    {
    }

    public DbSet<Story> Stories => Set<Story>();
    public DbSet<ContextAsset> ContextAssets => Set<ContextAsset>();
    public DbSet<StoryConversation> StoryConversations => Set<StoryConversation>();
    public DbSet<StoryMessage> StoryMessages => Set<StoryMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var story = modelBuilder.Entity<Story>();
        story.HasKey(s => s.Id);
        story.Property(s => s.UserId).IsRequired();
        story.Property(s => s.Title).IsRequired().HasMaxLength(512);
        story.Property(s => s.Description).HasMaxLength(4000);
        story.Property(s => s.Points).IsRequired();

        story.Property(s => s.Area).HasMaxLength(256);
        story.Property(s => s.Iteration).HasMaxLength(256);
        story.Property(s => s.State).HasMaxLength(128);
        story.Property(s => s.AssignedTo).HasMaxLength(256);
        story.Property(s => s.Priority);
        story.Property(s => s.Risk).HasMaxLength(128);
        story.Property(s => s.TargetDate);
        story.Property(s => s.AcceptanceCriteria).HasMaxLength(4000);
        story.Property(s => s.RelatedWorkItem).HasMaxLength(256);
        story.Property(s => s.UseCase).HasMaxLength(1024);

        story.Property(s => s.CreatedAt).IsRequired();
        story.Property(s => s.UpdatedAt).IsRequired();

        story.HasIndex(s => new { s.UserId, s.CreatedAt });
        story.HasIndex(s => new { s.UserId, s.UpdatedAt });
        story.HasIndex(s => new { s.UserId, s.State });
        story.HasIndex(s => new { s.UserId, s.Area });
        story.HasIndex(s => new { s.UserId, s.Iteration });

        var asset = modelBuilder.Entity<ContextAsset>();
        asset.HasKey(a => a.Id);
        asset.Property(a => a.UserId).IsRequired();
        asset.Property(a => a.FileName).IsRequired().HasMaxLength(512);
        asset.Property(a => a.ContentType).HasMaxLength(256);
        asset.Property(a => a.StoragePath).IsRequired().HasMaxLength(1024);
        asset.Property(a => a.TextExtract).HasMaxLength(100000);
        asset.Property(a => a.CreatedAt).IsRequired();
        asset.Property(a => a.UpdatedAt).IsRequired();

        asset.HasIndex(a => new { a.UserId, a.CreatedAt });
        asset.HasIndex(a => new { a.UserId, a.FileName });

        var conv = modelBuilder.Entity<StoryConversation>();
        conv.HasKey(c => c.Id);
        conv.Property(c => c.StoryId).IsRequired();
        conv.Property(c => c.CreatedAt).IsRequired();
        conv.Property(c => c.UpdatedAt).IsRequired();
        conv.HasIndex(c => c.StoryId).IsUnique();

        var msg = modelBuilder.Entity<StoryMessage>();
        msg.HasKey(m => m.Id);
        msg.Property(m => m.StoryId).IsRequired();
        msg.Property(m => m.Role).IsRequired().HasMaxLength(32);
        msg.Property(m => m.Content).IsRequired();
        msg.Property(m => m.CreatedAt).IsRequired();
        msg.HasIndex(m => new { m.StoryId, m.CreatedAt });
    }
}
