using System.Text.RegularExpressions;
using KhataFlow.Core.ServiceContracts;

namespace KhataFlow.Core.Services;

public class BilingualTextService : IBilingualTextService
{
    private readonly IAIClientService _aiClient;

    private static readonly Regex UrduScriptPattern = new(
        @"[\u0600-\u06FF\u0750-\u077F\uFB50-\uFDFF\uFE70-\uFEFF]",
        RegexOptions.Compiled);

    public BilingualTextService(IAIClientService aiClient)
    {
        _aiClient = aiClient;
    }

    public bool ContainsUrduScript(string? text) =>
        !string.IsNullOrWhiteSpace(text) && UrduScriptPattern.IsMatch(text);

    public async Task<(string English, string Urdu)> ResolveAsync(string input, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input);

        if (ContainsUrduScript(input))
        {
            var english = await _aiClient.TranslateAsync(input, "en", ct);
            return (english, input);
        }

        var urdu = await _aiClient.TranslateAsync(input, "ur", ct);
        return (input, urdu);
    }

    public bool IsTranslationStale(string? source, string? translated) =>
        string.IsNullOrWhiteSpace(translated)
        || string.Equals(source, translated, StringComparison.Ordinal);
}