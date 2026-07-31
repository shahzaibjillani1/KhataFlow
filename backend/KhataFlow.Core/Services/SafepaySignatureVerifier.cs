using System.Security.Cryptography;
using System.Text;
using KhataFlow.Core.Configuration;
using Microsoft.Extensions.Options;

namespace KhataFlow.Core.Services;

public class SafepaySignatureVerifier
{
    private readonly SafepayOptions _options;

    public SafepaySignatureVerifier(IOptions<SafepayOptions> options) => _options = options.Value;

    // Confirmed against Safepay's "Verify HMAC signatures" docs: HMAC-SHA512 over the raw
    // webhook request body bytes (not a parsed/re-serialized copy — use the exact bytes as
    // received), hex-encoded, signed with the Webhook Secret (WebhookSecret, not SecretKey),
    // compared against the X-SFPY-SIGNATURE header.
    public bool IsValid(string rawBody, string signatureHeader)
    {
        if (string.IsNullOrEmpty(rawBody) || string.IsNullOrEmpty(signatureHeader))
            return false;

        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(rawBody));
        var expected = Convert.ToHexString(hash).ToLowerInvariant();

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signatureHeader.ToLowerInvariant()));
    }
}