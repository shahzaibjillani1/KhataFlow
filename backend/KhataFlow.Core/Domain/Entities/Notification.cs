using KhataFlow.Core.Domain.Common;
using KhataFlow.Core.Domain.IdentityEntities;
using KhataFlow.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace KhataFlow.Core.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public Guid? BusinessId { get; set; }
    public Business? Business { get; set; }

    public NotificationTarget Target { get; set; } = NotificationTarget.User;

    [Required, MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(100)]
    public string? TitleUr { get; set; }       

    [Required, MaxLength(500)]
    public string Message { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? MessageUr { get; set; }    

    public NotificationType Type { get; set; } = NotificationType.System;

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public Guid? ReferenceId { get; set; }
}