namespace KhataFlow.Infrastructure.ExternalServices.Groq;

public class GroqOptions
{
    public const string SectionName = "Groq";

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-oss-120b";
    public string TranslationModel { get; set; } = "openai/gpt-oss-20b";
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/chat/completions";
    public string TranscriptionModel { get; set; } = "whisper-large-v3";
    public string TranscriptionUrl { get; set; } = "https://api.groq.com/openai/v1/audio/transcriptions";
}