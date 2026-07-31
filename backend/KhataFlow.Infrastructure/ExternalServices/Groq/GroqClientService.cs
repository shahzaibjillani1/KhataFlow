using System.Text;
using System.Text.Json;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KhataFlow.Infrastructure.ExternalServices.Groq;

public class GroqClientService : IAIClientService
{
    private readonly HttpClient _http;
    private readonly GroqOptions _options;
    private readonly ILogger<GroqClientService> _logger;

    public GroqClientService(
        HttpClient http,
        IOptions<GroqOptions> options,
        ILogger<GroqClientService> logger
    )
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateFromAudioAsync(
        byte[] audioBytes,
        string mimeType,
        string prompt,
        CancellationToken ct = default
    )
    {
        var transcript = await TranscribeAsync(audioBytes, mimeType, ct);

        if (string.IsNullOrWhiteSpace(transcript))
            throw new KhataFlow.Core.Exceptions.AIServiceUnavailableException(
                "Could not transcribe audio."
            );

        // Reuse the exact same intent-extraction prompt, just against transcribed text
        var fullPrompt = prompt + $"\n\nUser said: \"{transcript}\"";
        return await GenerateFromTextAsync(fullPrompt, ct);
    }

    private async Task<string> TranscribeAsync(
        byte[] audioBytes,
        string mimeType,
        CancellationToken ct
    )
    {
        using var form = new MultipartFormDataContent();

        // Browsers (e.g. MediaRecorder in Chrome) report mimeType with codec
        // parameters attached, like "audio/webm;codecs=opus". MediaTypeHeaderValue's
        // constructor only accepts a bare "type/subtype" and throws a FormatException
        // on anything with parameters — so strip everything from the first ';' onward
        // before constructing the header.
        var baseMimeType = mimeType.Split(';')[0].Trim();

        var audioContent = new ByteArrayContent(audioBytes);
        audioContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            baseMimeType
        );
        form.Add(audioContent, "file", "audio" + ExtensionFor(baseMimeType));
        form.Add(new StringContent(_options.TranscriptionModel), "model");
        form.Add(new StringContent("json"), "response_format");
        // Optional: hint the language mix you support
        // form.Add(new StringContent("ur"), "language"); // omit to let Whisper auto-detect Urdu/English/Roman Urdu

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TranscriptionUrl)
        {
            Content = form,
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            _options.ApiKey
        );

        using var response = await _http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Groq transcription failed. Status: {Status}. Body: {Body}",
                response.StatusCode,
                responseBody
            );

            if (
                response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
            )
            {
                throw new KhataFlow.Core.Exceptions.AIServiceUnavailableException(
                    "Voice transcription is temporarily overloaded. Please try again shortly."
                );
            }

            throw new HttpRequestException($"Groq transcription error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.TryGetProperty("text", out var textEl)
            ? textEl.GetString() ?? ""
            : "";
    }

    private static string ExtensionFor(string mimeType) =>
        mimeType switch
        {
            "audio/webm" => ".webm",
            "audio/mp4" or "audio/m4a" => ".m4a",
            "audio/wav" or "audio/x-wav" => ".wav",
            "audio/ogg" => ".ogg",
            _ => ".webm",
        };

    // Interface-facing overload: uses the default (intent-extraction) model.
    public Task<string> GenerateFromTextAsync(string prompt, CancellationToken ct = default) =>
        GenerateFromTextAsync(prompt, _options.Model, ct);

    // Extra overload for callers (like TranslateAsync) that want a specific model.
    public async Task<string> GenerateFromTextAsync(
        string prompt,
        string model,
        CancellationToken ct = default
    )
    {
        var payload = new
        {
            model,
            messages = new object[] { new { role = "user", content = prompt } },
            temperature = 0.1,
            response_format = new { type = "json_object" },
        };

        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.BaseUrl)
        {
            Content = content,
        };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            _options.ApiKey
        );

        using var response = await _http.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Groq API call failed. Status: {Status}. Body: {Body}",
                response.StatusCode,
                responseBody
            );

            if (
                response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
                || response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
            )
            {
                throw new KhataFlow.Core.Exceptions.AIServiceUnavailableException(
                    "The AI service is temporarily overloaded. Please try again in a moment."
                );
            }

            throw new HttpRequestException($"Groq API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
            ?? string.Empty;
    }

    public async Task<string> TranslateAsync(
        string text,
        string targetLanguage,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalizedTarget = targetLanguage.Trim().ToLowerInvariant();
        if (normalizedTarget != "ur" && normalizedTarget != "en")
            return text; // unsupported target language, no-op

        var (sourceLabel, targetLabel) =
            normalizedTarget == "ur" ? ("English", "Urdu") : ("Urdu", "English");

        var prompt = $$"""
            Translate the following {{sourceLabel}} text to {{targetLabel}}. Preserve any numbers, currency
            symbols, and placeholders like {0} exactly as they appear — do not translate them.
            Respond ONLY as JSON in this exact form, with no other text: {"translation": "..."}

            Text: "{{text}}"
            """;

        try
        {
            // Use the cheaper/faster translation-specific model instead of the default one.
            var raw = await GenerateFromTextAsync(prompt, _options.TranslationModel, ct);
            using var doc = JsonDocument.Parse(raw);

            return doc.RootElement.TryGetProperty("translation", out var t)
                ? t.GetString() ?? text
                : text;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Groq translation failed, falling back to English text");
            return text;
        }
    }
}
