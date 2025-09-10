using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Core.Entities;

namespace WorkflowEngine.Infrastructure.Data;

public class WorkflowEngineDbContext : DbContext
{
    public WorkflowEngineDbContext(DbContextOptions<WorkflowEngineDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Workflow> Workflows { get; set; } = null!;
    public DbSet<WorkflowExecution> WorkflowExecutions { get; set; } = null!;
    public DbSet<ExecutionLog> ExecutionLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired();

            // Relationships
            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiresAt);
        });

        // Workflow configuration
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);

            // PostgreSQL JSON columns
            entity.Property(e => e.NodesJson).HasColumnType("jsonb");
            entity.Property(e => e.ConnectionsJson).HasColumnType("jsonb");
            entity.Property(e => e.SettingsJson).HasColumnType("jsonb");

            // Enum conversion
            entity.Property(e => e.Status).HasConversion<string>();

            // Relationships
            entity.HasOne(e => e.Creator)
                  .WithMany(u => u.Workflows)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Name);
        });

        // WorkflowExecution configuration
        modelBuilder.Entity<WorkflowExecution>(entity =>
        {
            entity.HasKey(e => e.Id);

            // JSON columns
            entity.Property(e => e.InputDataJson).HasColumnType("jsonb");
            entity.Property(e => e.OutputDataJson).HasColumnType("jsonb");
            entity.Property(e => e.ErrorDataJson).HasColumnType("jsonb");

            // Enum conversions
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.TriggerType).HasConversion<string>();

            // Relationships
            entity.HasOne(e => e.Workflow)
                  .WithMany(w => w.Executions)
                  .HasForeignKey(e => e.WorkflowId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
        });

        // ExecutionLog configuration
        modelBuilder.Entity<ExecutionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.DataJson).HasColumnType("jsonb");

            // Enum conversion
            entity.Property(e => e.Level).HasConversion<string>();

            // Relationships
            entity.HasOne(e => e.Execution)
                  .WithMany(ex => ex.Logs)
                  .HasForeignKey(e => e.ExecutionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            entity.HasIndex(e => e.ExecutionId);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.NodeId);
        });
    }
}