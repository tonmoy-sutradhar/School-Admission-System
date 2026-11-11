using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;

namespace Users.Application.Abstractions;

public interface IUsersDbContext
{
    DbSet<UserProfile> UserProfiles { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}