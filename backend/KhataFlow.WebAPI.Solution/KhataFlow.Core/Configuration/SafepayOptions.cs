namespace KhataFlow.Core.Configuration;

public class SafepayOptions
{
    public string SandboxBaseUrl { get; set; } = "https://sandbox.api.getsafepay.com";
    public string ProductionBaseUrl { get; set; } = "https://api.getsafepay.com";

    public string PublicKey { get; set; } = default!;

    public string SecretKey { get; set; } = default!;

    public string WebhookSecret { get; set; } = default!;

    public bool UseSandbox { get; set; } = true;

    public string BaseUrl => UseSandbox ? SandboxBaseUrl : ProductionBaseUrl;
}