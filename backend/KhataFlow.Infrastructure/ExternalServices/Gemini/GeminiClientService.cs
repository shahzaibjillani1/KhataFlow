using System.Text;
using System.Text.Json;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KhataFlow.Infrastructure.ExternalServices.Gemini;

public class GeminiClientService : IAIClientService
{
    private readonly HttpClient _http;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiClientService> _logger;

    public GeminiClientService(
        HttpClient http,
        IOptions<GeminiOptions> options,
        ILogger<GeminiClientService> logger
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
        var payload = new
        {
            contents = new object[]
            {
                new
                {
                    parts = new object[]
                    {
                        new { text = prompt },
                        new
                        {
                            inline_data = new
                            {
                                mime_type = mimeType,
                                data = Convert.ToBase64String(audioBytes),
                            },
                        },
                    },
                },
            },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.1,
                maxOutputTokens = 2048,

                thinkingConfig = new { thinkingLevel = "low" },
            },
        };

        return await SendAsync(payload, ct);
    }

    public async Task<string> GenerateFromTextAsync(string prompt, CancellationToken ct = default)
    {
        var payload = new
        {
            contents = new object[] { new { parts = new object[] { new { text = prompt } } } },
            generationConfig = new
            {
                response_mime_type = "application/json",
                temperature = 0.1,
                maxOutputTokens = 2048,
                thinkingConfig = new { thinkingLevel = "low" },
            },
        };

        return await SendAsync(payload, ct);
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

        string raw;
        try
        {
            raw = await GenerateFromTextAsync(prompt, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Gemini translation generation failed, falling back to English text"
            );
            return text;
        }

        var json = ExtractJsonObject(raw);
        if (json is null)
        {
            _logger.LogWarning(
                "Gemini translation response had no parseable JSON object. Raw: {Raw}",
                raw
            );
            return text;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("translation", out var t)
                ? t.GetString() ?? text
                : text;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Gemini translation response was not valid JSON. Raw: {Raw}",
                raw
            );
            return text;
        }
    }

    private async Task<string> SendAsync(object payload, CancellationToken ct)
    {
        var url = $"{_options.BaseUrl}/{_options.Model}:generateContent?key={_options.ApiKey}";
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await _http.PostAsync(url, content, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Gemini API call failed. Status: {Status}. Body: {Body}",
                response.StatusCode,
                responseBody
            );
            throw new HttpRequestException($"Gemini API error: {response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(responseBody);

        var candidates = doc.RootElement.GetProperty("candidates");
        if (candidates.GetArrayLength() == 0)
        {
            _logger.LogError("Gemini returned no candidates. Body: {Body}", responseBody);
            throw new HttpRequestException("Gemini API error: no candidates returned");
        }

        var parts = candidates[0].GetProperty("content").GetProperty("parts");

        var sb = new StringBuilder();
        foreach (var part in parts.EnumerateArray())
        {
            if (
                part.TryGetProperty("thought", out var thoughtFlag)
                && thoughtFlag.ValueKind == JsonValueKind.True
            )
            {
                continue;
            }

            if (part.TryGetProperty("text", out var textEl))
                sb.Append(textEl.GetString());
        }

        var result = sb.ToString();

        if (string.IsNullOrWhiteSpace(result))
        {
            _logger.LogError(
                "Gemini returned only thought content, no final answer. Body: {Body}",
                responseBody
            );
            throw new HttpRequestException("Gemini API error: empty final response");
        }

        return result;
    }

    private static string? ExtractJsonObject(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var cleaned = raw.Trim();

        if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline >= 0)
                cleaned = cleaned[(firstNewline + 1)..];

            var fenceEnd = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (fenceEnd >= 0)
                cleaned = cleaned[..fenceEnd];

            cleaned = cleaned.Trim();
        }

        int start = cleaned.IndexOf('{');
        if (start < 0)
            return null;

        int depth = 0;
        for (int i = start; i < cleaned.Length; i++)
        {
            if (cleaned[i] == '{')
                depth++;
            else if (cleaned[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return cleaned[start..(i + 1)];
            }
        }

        return null;
    }
}
