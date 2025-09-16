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
    public DbSet<Organization> Organizations { get; set; } = null!;
    public DbSet<OrganizationMember> OrganizationMembers { get; set; } = null!;
    public DbSet<OrganizationInvite> OrganizationInvites { get; set; } = null!;
    public DbSet<WorkflowPermission> WorkflowPermissions { get; set; } = null!;
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; } = null!;
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUserEntities(modelBuilder);
        ConfigureWorkflowEntities(modelBuilder);
        ConfigureOrganizationEntities(modelBuilder);
        ConfigureIndexes(modelBuilder);
    }

    private void ConfigureUserEntities(ModelBuilder modelBuilder)
    {
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.TimeZone).HasMaxLength(50);

            // Multi-tenant relationship
            entity.HasOne(e => e.CurrentOrganization)
                  .WithMany()
                  .HasForeignKey(e => e.CurrentOrganizationId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.CurrentOrganizationId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TokenHash).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.TokenHash);
            entity.HasIndex(e => e.ExpiresAt);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.EmailVerificationTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsUsed });
        });

        // Password Reset Token configuration  
        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired();
            entity.Property(e => e.IpAddress).HasMaxLength(45); // IPv6 support
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.PasswordResetTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsUsed });
        });
    }

    private void ConfigureWorkflowEntities(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Workflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);

            // PostgreSQL JSON columns
            entity.Property(e => e.NodesJson).HasColumnType("jsonb");
            entity.Property(e => e.ConnectionsJson).HasColumnType("jsonb");
            entity.Property(e => e.SettingsJson).HasColumnType("jsonb");

            // Enum conversions
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.Visibility).HasConversion<string>();

            // Multi-tenant relationships
            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Workflows)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Creator)
                  .WithMany(u => u.CreatedWorkflows)
                  .HasForeignKey(e => e.CreatedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.OrganizationId);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => new { e.OrganizationId, e.Name });
        });

        modelBuilder.Entity<WorkflowExecution>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.InputDataJson).HasColumnType("jsonb");
            entity.Property(e => e.OutputDataJson).HasColumnType("jsonb");
            entity.Property(e => e.ErrorDataJson).HasColumnType("jsonb");

            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.TriggerType).HasConversion<string>();

            entity.HasOne(e => e.Workflow)
                  .WithMany(w => w.Executions)
                  .HasForeignKey(e => e.WorkflowId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.WorkflowId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartedAt);
        });

        modelBuilder.Entity<ExecutionLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.DataJson).HasColumnType("jsonb");

            entity.Property(e => e.Level).HasConversion<string>();

            entity.HasOne(e => e.Execution)
                  .WithMany(ex => ex.Logs)
                  .HasForeignKey(e => e.ExecutionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ExecutionId);
            entity.HasIndex(e => e.Level);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.NodeId);
        });

        modelBuilder.Entity<WorkflowPermission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Permission).HasConversion<string>();

            entity.HasOne(e => e.Workflow)
                  .WithMany(w => w.Permissions)
                  .HasForeignKey(e => e.WorkflowId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany()
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Granter)
                  .WithMany()
                  .HasForeignKey(e => e.GrantedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one permission per user per workflow
            entity.HasIndex(e => new { e.WorkflowId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.GrantedBy);
        });
    }

    private void ConfigureOrganizationEntities(ModelBuilder modelBuilder)
    {
        // Organization configuration
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Slug).HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Domain).HasMaxLength(50);

            entity.Property(e => e.Plan).HasConversion<string>();

            entity.HasIndex(e => e.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
            entity.HasIndex(e => e.Domain).IsUnique().HasFilter("\"Domain\" IS NOT NULL");
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedAt);
        });

        modelBuilder.Entity<OrganizationMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).HasConversion<string>();

            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Members)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Organizations)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Inviter)
                  .WithMany()
                  .HasForeignKey(e => e.InvitedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint: one membership per user per organization
            entity.HasIndex(e => new { e.OrganizationId, e.UserId }).IsUnique();
            entity.HasIndex(e => e.Role);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.JoinedAt);
        });

        modelBuilder.Entity<OrganizationInvite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(320);
            entity.Property(e => e.InviteToken).IsRequired();

            entity.Property(e => e.Role).HasConversion<string>();
            entity.Property(e => e.Status).HasConversion<string>();

            entity.HasOne(e => e.Organization)
                  .WithMany(o => o.Invites)
                  .HasForeignKey(e => e.OrganizationId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Inviter)
                  .WithMany(u => u.SentInvites)
                  .HasForeignKey(e => e.InvitedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AcceptedByUser)
                  .WithMany()
                  .HasForeignKey(e => e.AcceptedBy)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.InviteToken).IsUnique();
            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.OrganizationId, e.Email });
        });
    }

    private void ConfigureIndexes(ModelBuilder modelBuilder)
    {

        // Multi-tenant workflow queries
        modelBuilder.Entity<Workflow>()
            .HasIndex(e => new { e.OrganizationId, e.Status, e.CreatedAt });

        // User organization membership queries
        modelBuilder.Entity<OrganizationMember>()
            .HasIndex(e => new { e.UserId, e.IsActive });

        // Workflow execution queries
        modelBuilder.Entity<WorkflowExecution>()
            .HasIndex(e => new { e.WorkflowId, e.Status, e.StartedAt });

        // Invite cleanup queries
        modelBuilder.Entity<OrganizationInvite>()
            .HasIndex(e => new { e.Status, e.ExpiresAt });
    }
}