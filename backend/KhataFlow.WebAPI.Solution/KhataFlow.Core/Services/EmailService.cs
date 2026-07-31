using KhataFlow.Core.ServiceContracts;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace KhataFlow.Core.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        var host = _config["EmailSettings:SmtpServer"]
            ?? throw new InvalidOperationException("SMTP server is not configured.");
        var port = int.Parse(_config["EmailSettings:Port"]
            ?? throw new InvalidOperationException("SMTP port is not configured."));
        var username = _config["EmailSettings:Username"]
            ?? throw new InvalidOperationException("SMTP username is not configured.");
        var password = _config["EmailSettings:Password"]
            ?? throw new InvalidOperationException("SMTP password is not configured.");
        var from = _config["EmailSettings:From"]
            ?? throw new InvalidOperationException("SMTP from address is not configured.");

        using var smtp = new SmtpClient      
        {
            Host = host,
            Port = port,
            EnableSsl = true,
            Credentials = new NetworkCredential(username, password)
        };

        using var message = new MailMessage(from, to, subject, body)
        {
            IsBodyHtml = true
        };

        await smtp.SendMailAsync(message);
    }
}
