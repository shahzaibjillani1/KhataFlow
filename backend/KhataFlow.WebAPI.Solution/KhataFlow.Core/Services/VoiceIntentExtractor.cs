using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Enums;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KhataFlow.Core.Services;

public class VoiceIntentExtractor
{
    private readonly IAIClientService _aiClient;
    private readonly ILogger<VoiceIntentExtractor> _logger;

    public VoiceIntentExtractor(IAIClientService aiClient, ILogger<VoiceIntentExtractor> logger)
    {
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<VoiceIntentResult?> ExtractAsync(byte[] audioBytes, string mimeType, CancellationToken ct = default)
    {
        var prompt = string.Format(VoiceIntentPrompts.SystemPrompt, DateTime.UtcNow.ToString("yyyy-MM-dd"));

        try
        {
            var raw = await _aiClient.GenerateFromAudioAsync(audioBytes, mimeType, prompt, ct);
            return Parse(raw);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to extract voice intent");
            return null;
        }
    }

    private VoiceIntentResult? Parse(string rawJson)
    {
        try
        {
            var trimmed = ExtractFirstJsonObject(rawJson);

            using var doc = JsonDocument.Parse(trimmed);
            var root = doc.RootElement;

            var intentStr = GetString(root, "intent") ?? "Unknown";
            Enum.TryParse<VoiceIntent>(intentStr, ignoreCase: true, out var intent);

            var result = new VoiceIntentResult
            {
                Intent = intent,
                CustomerName = GetString(root, "customerName"),
                PaymentMethod = GetString(root, "paymentMethod"),
                Amount = GetDecimal(root, "amount"),
                ExpenseCategory = GetString(root, "expenseCategory"),
                Description = GetString(root, "description"),
                ReportQuestion = GetString(root, "reportQuestion")
            };

            if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsEl.EnumerateArray())
                {
                    var name = GetString(item, "productName");
                    var qty = GetDecimal(item, "quantity");
                    if (!string.IsNullOrWhiteSpace(name) && qty is not null)
                        result.Items.Add(new VoiceIntentItem { ProductName = name, Quantity = qty.Value });
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "AI returned non-JSON or malformed JSON: {Raw}", rawJson);
            return null;
        }
    }

    private static string ExtractFirstJsonObject(string raw)
    {
        var start = raw.IndexOf('{');
        if (start < 0)
            return raw; 

        var depth = 0;
        for (var i = start; i < raw.Length; i++)
        {
            if (raw[i] == '{') depth++;
            else if (raw[i] == '}')
            {
                depth--;
                if (depth == 0)
                    return raw[start..(i + 1)]; 
            }
        }

        return raw[start..]; 
    }

    private static string? GetString(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var val) && val.ValueKind != JsonValueKind.Null ? val.GetString() : null;

    private static decimal? GetDecimal(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetDecimal() : null;
}