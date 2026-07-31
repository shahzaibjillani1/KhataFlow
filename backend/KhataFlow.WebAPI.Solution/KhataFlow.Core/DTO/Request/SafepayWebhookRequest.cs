using System.Text.Json.Serialization;

namespace KhataFlow.Core.DTO.Request;


public class SafepayWebhookRequest
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = string.Empty; 

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; 

    [JsonPropertyName("data")]
    public SafepayWebhookData Data { get; set; } = new();
}

public class SafepayWebhookData
{
    [JsonPropertyName("tracker")]
    public string? Tracker { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
}