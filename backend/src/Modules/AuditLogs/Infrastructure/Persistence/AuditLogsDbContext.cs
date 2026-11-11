using AuditLogs.Application.Abstractions;
using AuditLogs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditLogs.Infrastructure.Persistence;

public sealed class AuditLogsDbContext(DbContextOptions<AuditLogsDbContext> options)
    : DbContext(options), IAuditLogsDbContext
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("audit");
        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("audit_logs");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Action).HasMaxLength(150).IsRequired();
            builder.Property(x => x.EntityName).HasMaxLength(150).IsRequired();
            builder.Property(x => x.EntityId).HasMaxLength(100);
            builder.Property(x => x.Details).HasMaxLength(4000);
            builder.Property(x => x.IpAddress).HasMaxLength(64);
            builder.HasIndex(x => x.TimestampUtc);
            builder.HasIndex(x => x.UserId);
            builder.Ignore(x => x.DomainEvents);
        });
        base.OnModelCreating(modelBuilder);
    }
}