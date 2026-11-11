using Admissions.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Admissions.Application.Abstractions;

public interface IAdmissionsDbContext
{
    DbSet<AdmissionClass> AdmissionClasses { get; }
    DbSet<SeatHold> SeatHolds { get; }
    DatabaseFacade Database { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
