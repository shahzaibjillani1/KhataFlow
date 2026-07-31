namespace KhataFlow.Core.ServiceContracts;

public interface IBilingualTextService
{
    bool ContainsUrduScript(string? text);

    Task<(string English, string Urdu)> ResolveAsync(string input, CancellationToken ct = default);

    bool IsTranslationStale(string? source, string? translated);
}