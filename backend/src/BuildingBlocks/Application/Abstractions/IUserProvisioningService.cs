namespace BuildingBlocks.Application.Abstractions;

public interface IUserProvisioningService
{
    Task ProvisionNewUserAsync(
        Guid userId,
        string email,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);
}