using KhataFlow.Core.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace KhataFlow.Core.DTO.Response;

public class VoiceIntentResult
{
    public VoiceIntent Intent { get; set; } = VoiceIntent.Unknown;
    public string? CustomerName { get; set; }
    public string? PaymentMethod { get; set; } 
    public decimal? Amount { get; set; }
    public List<VoiceIntentItem> Items { get; set; } = new();
    public string? ExpenseCategory { get; set; }
    public string? Description { get; set; }
    public string? ReportQuestion { get; set; }
}
