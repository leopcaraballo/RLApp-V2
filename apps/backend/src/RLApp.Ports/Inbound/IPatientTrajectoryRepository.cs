namespace RLApp.Ports.Inbound;

using RLApp.Domain.Aggregates;

public interface IPatientTrajectoryRepository
{
    Task<PatientTrajectory> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PatientTrajectory?> FindActiveAsync(string patientId, string queueId, CancellationToken cancellationToken = default);
    Task AddAsync(PatientTrajectory trajectory, CancellationToken cancellationToken = default);
    Task UpdateAsync(PatientTrajectory trajectory, CancellationToken cancellationToken = default);
}
