using BuildingBlocks.Application.Abstractions;
using BuildingBlocks.Application.Common.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Users.Application.Abstractions;
using Users.Application.DTOs;
using Users.Domain.Entities;

namespace Users.Application.Services;

public sealed class UserService(
    IUsersDbContext db,
    ICurrentUserService currentUser,
    ICacheService cache,
    IAuditLogger auditLogger,
    IValidator<UpdateProfileRequest> validator)
{
    public async Task EnsureProfileAsync(CreateUserProfileMessage message, CancellationToken ct = default)
    {
        if (await db.UserProfiles.AnyAsync(x => x.UserId == message.UserId, ct))
            return;

        db.UserProfiles.Add(UserProfile.Create(message.UserId, message.Email, message.FirstName, message.LastName));
        await db.SaveChangesAsync(ct);
    }

    public async Task<Result<UserProfileDto>> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var cacheKey = $"users:profile:{userId}";
        var cached = await cache.GetAsync<UserProfileDto>(cacheKey, ct);
        if (cached is not null) return Result.Success(cached);

        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is null)
            return Result.Failure<UserProfileDto>(Error.NotFound("User profile not found."));

        var dto = Map(profile);
        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), ct);
        return Result.Success(dto);
    }

    public async Task<Result<UserProfileDto>> GetMeAsync(CancellationToken ct = default)
    {
        if (currentUser.UserId is null)
            return Result.Failure<UserProfileDto>(Error.Unauthorized("Not authenticated."));
        return await GetByIdAsync(currentUser.UserId.Value, ct);
    }

    public async Task<Result<UserProfileDto>> UpdateMeAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        await validator.ValidateAndThrowAsync(request, ct);
        if (currentUser.UserId is null)
            return Result.Failure<UserProfileDto>(Error.Unauthorized("Not authenticated."));

        var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == currentUser.UserId, ct);
        if (profile is null)
            return Result.Failure<UserProfileDto>(Error.NotFound("User profile not found."));

        profile.Update(request.FirstName, request.LastName, request.PhoneNumber, request.Bio, request.AvatarUrl);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync($"users:profile:{profile.UserId}", ct);
        await auditLogger.LogAsync("ProfileUpdated", nameof(UserProfile), profile.Id.ToString(), cancellationToken: ct);
        return Result.Success(Map(profile));
    }

    public async Task<Result<PagedResult<UserProfileDto>>> GetPagedAsync(PaginationRequest request, CancellationToken ct = default)
    {
        var query = db.UserProfiles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Email.Contains(s) ||
                x.FirstName.ToLower().Contains(s) ||
                x.LastName.ToLower().Contains(s));
        }

        query = (request.SortBy?.ToLowerInvariant()) switch
        {
            "email" => request.SortDescending ? query.OrderByDescending(x => x.Email) : query.OrderBy(x => x.Email),
            "firstname" => request.SortDescending ? query.OrderByDescending(x => x.FirstName) : query.OrderBy(x => x.FirstName),
            "createdat" => request.SortDescending ? query.OrderByDescending(x => x.CreatedAtUtc) : query.OrderBy(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return Result.Success(PagedResult<UserProfileDto>.Create(
            items.Select(Map).ToList(), request.PageNumber, request.PageSize, total));
    }

    public async Task<Result> SetActiveAsync(Guid userId, bool isActive, CancellationToken ct = default)
    {
        var profile = await db.UserProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        if (profile is null)
            return Result.Failure(Error.NotFound("User profile not found."));

        if (isActive) profile.Activate(); else profile.Deactivate();
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync($"users:profile:{userId}", ct);
        await auditLogger.LogAsync(isActive ? "UserActivated" : "UserDeactivated", nameof(UserProfile), profile.Id.ToString(), cancellationToken: ct);
        return Result.Success();
    }

    private static UserProfileDto Map(UserProfile x) =>
        new(x.Id, x.UserId, x.Email, x.FirstName, x.LastName, x.PhoneNumber, x.Bio, x.AvatarUrl, x.IsActive, x.CreatedAtUtc);
}