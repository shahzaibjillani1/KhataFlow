using KhataFlow.Core.DTO.Response;

namespace KhataFlow.Core.ServiceContracts;

public interface ITransactionAIService
{
    Task<TransactionAIResponse> ExtractTransactionFromAudioAsync(byte[] audioBytes, string mimeType, CancellationToken ct = default);

    Task<TransactionAIResponse> ExtractTransactionFromTextAsync(string rawText, CancellationToken ct = default);
}



