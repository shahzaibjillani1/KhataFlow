using KhataFlow.Core.Exceptions;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KhataFlow.Infrastructure.ExternalServices;

public class FallbackAIClientService : IAIClientService
{
    private readonly IAIClientService _primary;
    private readonly IAIClientService _secondary;
    private readonly ILogger<FallbackAIClientService> _logger;

    public FallbackAIClientService(
        [FromKeyedServices("primary")] IAIClientService primary,
        [FromKeyedServices("secondary")] IAIClientService secondary,
        ILogger<FallbackAIClientService> logger
    )
    {
        _primary = primary;
        _secondary = secondary;
        _logger = logger;
    }

    public async Task<string> GenerateFromAudioAsync(
        byte[] audioBytes,
        string mimeType,
        string prompt,
        CancellationToken ct = default
    )
    {
        try
        {
            return await _primary.GenerateFromAudioAsync(audioBytes, mimeType, prompt, ct);
        }
        catch (Exception ex) when (ex is AIServiceUnavailableException or HttpRequestException)
        {
            _logger.LogWarning(
                ex,
                "Primary AI provider failed for audio request, attempting fallback"
            );

            try
            {
                return await _secondary.GenerateFromAudioAsync(audioBytes, mimeType, prompt, ct);
            }
            catch (NotSupportedException)
            {
                _logger.LogError(
                    "Fallback provider does not support audio input; no provider available"
                );
                throw new AIServiceUnavailableException(
                    "Voice processing is temporarily unavailable. Please try again shortly."
                );
            }
        }
    }

    public async Task<string> GenerateFromTextAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            return await _primary.GenerateFromTextAsync(prompt, ct);
        }
        catch (Exception ex) when (ex is AIServiceUnavailableException or HttpRequestException)
        {
            _logger.LogWarning(
                ex,
                "Primary AI provider failed for text request, attempting fallback"
            );
            return await _secondary.GenerateFromTextAsync(prompt, ct);
        }
    }

    public async Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        try
        {
            return await _primary.TranslateAsync(text, targetLanguage, ct);
        }
        catch (Exception ex) when (ex is AIServiceUnavailableException or HttpRequestException)
        {
            _logger.LogWarning(
                ex,
                "Primary AI provider failed for translation, attempting fallback"
            );
            return await _secondary.TranslateAsync(text, targetLanguage, ct);
        }
    }
}
