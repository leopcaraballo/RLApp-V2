namespace RLApp.Ports.Inbound;

using RLApp.Domain.Aggregates;

/// <summary>
/// Port for consulting room repository.
/// Reference: S-002 Consulting Room Lifecycle, UC-003, UC-005, UC-006
/// Implements: ADR-001 (Hexagonal Architecture)
/// </summary>
public interface IConsultingRoomRepository
{
    /// <summary>
    /// Get a consulting room by ID.
    /// </summary>
    Task<ConsultingRoom> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new consulting room.
    /// </summary>
    Task AddAsync(ConsultingRoom consultingRoom, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing consulting room.
    /// </summary>
    Task UpdateAsync(ConsultingRoom consultingRoom, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all consulting rooms.
    /// </summary>
    Task<IList<ConsultingRoom>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active consulting rooms.
    /// </summary>
    Task<IList<ConsultingRoom>> GetActiveAsync(CancellationToken cancellationToken = default);
}
