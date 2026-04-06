namespace RLApp.Ports.Outbound;

/// <summary>
/// Port for projection store (read model persistence).
/// Reference: ADR-006 Persistent Projections
/// </summary>
public interface IProjectionStore
{
    Task UpsertAsync(string projectionId, string projectionType, object projectionData, CancellationToken cancellationToken = default);
    Task<T> GetAsync<T>(string projectionId, CancellationToken cancellationToken = default) where T : class;
    Task<PatientTrajectoryProjection?> GetPatientTrajectoryAsync(string trajectoryId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string projectionId, CancellationToken cancellationToken = default);
}
