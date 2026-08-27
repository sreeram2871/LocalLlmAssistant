using LocalLlmAssistant.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LocalLlmAssistant.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chat> Chats => Set<Chat>();

    public DbSet<ChatMessageEntity> ChatMessages =>
        Set<ChatMessageEntity>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Chat
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.Property(x => x.UpdatedAt)
                .IsRequired();

            entity.HasMany(x => x.Messages)
                .WithOne(x => x.Chat)
                .HasForeignKey(x => x.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Chat Message
        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.Role)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(x => x.Content)
                .IsRequired();

            entity.Property(x => x.CreatedAt)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.ChatId,
                x.CreatedAt
            });
        });
    }
}