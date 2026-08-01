using KhataFlow.Core.DTO.Request;
using KhataFlow.Core.DTO.Response;
using KhataFlow.Core.Resources;
using KhataFlow.Core.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace KhataFlow.WebAPI.Controllers.v1;

[Authorize]
public class AIController : CustomControllerBase
{
    private readonly ITransactionAIService _transactionAiService;
    private readonly IVoiceOrchestrationService _voiceOrchestrationService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private const long MaxAudioBytes = 10 * 1024 * 1024;

    public AIController(
        ITransactionAIService transactionAiService,
        IVoiceOrchestrationService voiceOrchestrationService,
        IStringLocalizer<SharedResource> localizer)
        : base(localizer)
    {
        _transactionAiService = transactionAiService;
        _voiceOrchestrationService = voiceOrchestrationService;
        _localizer = localizer;
    }

    [HttpPost("voice-command")]
    [RequestSizeLimit(MaxAudioBytes)]
    public async Task<IActionResult> VoiceCommand(IFormFile audio, CancellationToken ct)
    {
        if (audio is null || audio.Length == 0)
            return BadRequestResponse(_localizer["Ai.Voice.NoAudioReceived"]);

        if (audio.Length > MaxAudioBytes)
            return BadRequestResponse(_localizer["Ai.Voice.AudioTooLarge"]);

        var businessId = GetBusinessId();

        using var ms = new MemoryStream();
        await audio.CopyToAsync(ms, ct);

        var result = await _voiceOrchestrationService.ProcessVoiceCommandAsync(
            ms.ToArray(),
            string.IsNullOrWhiteSpace(audio.ContentType) ? "audio/webm" : audio.ContentType,
            businessId,
            ct);

        if (!result.Success)
            return BadRequestResponse(result.ErrorMessage ?? _localizer["Ai.Voice.CommandProcessingFailed"]);

        return Success(result, result.Message ?? _localizer["Ai.Voice.CommandProcessed"]);
    }
}