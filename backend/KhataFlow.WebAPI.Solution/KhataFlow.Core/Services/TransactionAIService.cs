using System.Text.Json;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Logging;

namespace KhataFlow.Core.Services
{
    public class TransactionAIService : ITransactionAIService
    {
        private readonly IAIClientService _aiClient;
        private readonly ILogger<TransactionAIService> _logger;

        private const string SystemPrompt = @"
You are an accounting assistant for a bookkeeping app called KhataFlow.
Listen to the user's voice note (may be in English, Urdu, or Roman Urdu) and extract:

- transactionType: one of ""Expense"", ""Income"", ""Udhar"" (credit given/received)
- amount: number only, no currency symbol
- currency: default ""PKR"" unless stated otherwise
- person: the name mentioned, or null if none
- category: a short category like ""Groceries"", ""Rent"", ""Salary"", ""Utilities""
- date: ISO 8601 date. Resolve relative terms (""yesterday"", ""today"") against {0}. Default to {0} if not mentioned.
- description: a short one-line summary of the transaction

Return ONLY valid JSON, no markdown, no commentary, matching exactly this shape:
{{
  ""transactionType"": string,
  ""amount"": number,
  ""currency"": string,
  ""person"": string | null,
  ""category"": string,
  ""date"": string,
  ""description"": string
}}

If a field cannot be determined, set it to null.";

        public TransactionAIService(IAIClientService aiClient, ILogger<TransactionAIService> logger)
        {
            _aiClient = aiClient;
            _logger = logger;
        }

        public async Task<TransactionAIResponse> ExtractTransactionFromAudioAsync(byte[] audioBytes, string mimeType, CancellationToken ct = default)
        {
            var prompt = string.Format(SystemPrompt, DateTime.UtcNow.ToString("yyyy-MM-dd"));

            try
            {
                var raw = await _aiClient.GenerateFromAudioAsync(audioBytes, mimeType, prompt, ct);
                return ParseResult(raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract transaction from audio");
                return new TransactionAIResponse { Success = false, ErrorMessage = "Could not process voice note." };
            }
        }

        public async Task<TransactionAIResponse> ExtractTransactionFromTextAsync(string rawText, CancellationToken ct = default)
        {
            var prompt = string.Format(SystemPrompt, DateTime.UtcNow.ToString("yyyy-MM-dd"))
                         + $"\n\nUser text: \"{rawText}\"";

            try
            {
                var raw = await _aiClient.GenerateFromTextAsync(prompt, ct);
                return ParseResult(raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract transaction from text");
                return new TransactionAIResponse { Success = false, ErrorMessage = "Could not process input." };
            }
        }

        private TransactionAIResponse ParseResult(string rawJson)
        {
            try
            {
                using var doc = JsonDocument.Parse(rawJson);
                var root = doc.RootElement;

                var result = new TransactionAIResponse
                {
                    Success = true,
                    TransactionType = GetString(root, "transactionType"),
                    Amount = GetDecimal(root, "amount"),
                    Currency = GetString(root, "currency") ?? "PKR",
                    Person = GetString(root, "person"),
                    Category = GetString(root, "category"),
                    Description = GetString(root, "description")
                };

                if (DateTime.TryParse(GetString(root, "date"), out var parsedDate))
                    result.Date = parsedDate;

                return result;
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "AI returned non-JSON or malformed JSON: {Raw}", rawJson);
                return new TransactionAIResponse { Success = false, ErrorMessage = "AI response could not be parsed." };
            }
        }

        private static string? GetString(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var val) && val.ValueKind != JsonValueKind.Null ? val.GetString() : null;

        private static decimal? GetDecimal(JsonElement el, string prop) =>
            el.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.Number ? val.GetDecimal() : null;
    }
}