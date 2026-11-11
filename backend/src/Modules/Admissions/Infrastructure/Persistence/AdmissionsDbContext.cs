using Admissions.Application.Abstractions;
using Admissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admissions.Infrastructure.Persistence;

public sealed class AdmissionsDbContext(DbContextOptions<AdmissionsDbContext> options)
    : DbContext(options), IAdmissionsDbContext
{
    public DbSet<AdmissionClass> AdmissionClasses => Set<AdmissionClass>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("admissions");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdmissionsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
