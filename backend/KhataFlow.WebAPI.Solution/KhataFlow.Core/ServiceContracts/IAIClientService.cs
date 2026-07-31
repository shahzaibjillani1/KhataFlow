namespace KhataFlow.Core.ServiceContracts;

public interface IAIClientService
{
    Task<string> GenerateFromAudioAsync(byte[] audioBytes, string mimeType, string prompt, CancellationToken ct = default);
    Task<string> GenerateFromTextAsync(string prompt, CancellationToken ct = default);
    Task<string> TranslateAsync(string text, string targetLanguage, CancellationToken ct = default);
}