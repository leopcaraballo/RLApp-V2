namespace RLApp.Ports.Inbound;

using RLApp.Domain.Aggregates;

/// <summary>
/// Port for staff user repository.
/// Reference: S-001 Staff Identity And Access
/// </summary>
public interface IStaffUserRepository
{
    Task<StaffUser> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<StaffUser> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<StaffUser> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(StaffUser staffUser, CancellationToken cancellationToken = default);
    Task UpdateAsync(StaffUser staffUser, CancellationToken cancellationToken = default);
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
