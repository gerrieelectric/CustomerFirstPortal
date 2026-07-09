using Microsoft.EntityFrameworkCore;
using Portal.Domain.Entities;
using Portal.Application.Infrastructure.Persistence.Configurations;

namespace Portal.Application.Infrastructure.Persistence;

public class PortalDbContext(DbContextOptions<PortalDbContext> options) : DbContext(options)
{
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<SubmissionAttachment> SubmissionAttachments => Set<SubmissionAttachment>();
    public DbSet<SubmissionReview> SubmissionReviews => Set<SubmissionReview>();
    public DbSet<SubmissionSuggestion> SubmissionSuggestions => Set<SubmissionSuggestion>();
    public DbSet<GerrieSyncEvent> GerrieSyncEvents => Set<GerrieSyncEvent>();
    public DbSet<NotificationDispatch> NotificationDispatches => Set<NotificationDispatch>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PortalDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
