using AuditLogs.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuditLogs.Application.Abstractions;

public interface IAuditLogsDbContext
{
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}