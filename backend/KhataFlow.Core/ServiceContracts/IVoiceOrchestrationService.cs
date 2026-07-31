using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface IVoiceOrchestrationService
{
    Task<VoiceCommandResponse> ProcessVoiceCommandAsync(
        byte[] audioBytes, string mimeType, Guid businessId, CancellationToken ct = default);
}