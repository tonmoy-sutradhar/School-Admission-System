using BuildingBlocks.Domain.Common;

namespace Identity.Domain.Events;

public sealed record UserRegisteredDomainEvent(Guid UserId, string Email) : DomainEvent;