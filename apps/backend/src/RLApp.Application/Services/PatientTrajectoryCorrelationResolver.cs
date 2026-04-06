namespace RLApp.Application.Services;

using RLApp.Domain.Common;
using RLApp.Ports.Inbound;

public sealed class PatientTrajectoryCorrelationResolver
{
    private readonly IPatientTrajectoryRepository _trajectoryRepository;

    public PatientTrajectoryCorrelationResolver(IPatientTrajectoryRepository trajectoryRepository)
    {
        _trajectoryRepository = trajectoryRepository;
    }

    public async Task<string> ResolveRequiredAsync(string patientId, string queueId, CancellationToken cancellationToken)
    {
        var trajectory = await _trajectoryRepository.FindActiveAsync(patientId, queueId, cancellationToken);
        if (trajectory is null)
        {
            throw new DomainException($"Active trajectory not found for patient {patientId} in queue {queueId}");
        }

        return trajectory.Id;
    }
}