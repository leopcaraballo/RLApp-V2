using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RLApp.Adapters.Http.Requests;

public sealed class RebuildPatientTrajectoriesRequest
{
    [JsonPropertyName("queueId")]
    public string? QueueId { get; set; }

    [JsonPropertyName("patientId")]
    public string? PatientId { get; set; }

    [Required]
    [JsonPropertyName("dryRun")]
    public bool DryRun { get; set; }
}
