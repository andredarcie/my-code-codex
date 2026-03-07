# EmailService

Implementação do `IEmailService` usando `SmtpClient`.

```csharp
namespace Infra.Services;

public class EmailService : IEmailService
{
    private readonly SmtpClient _smtp;

    public EmailService(SmtpClient smtp)
    {
        _smtp = smtp;
    }

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        var message = new MailMessage("no-reply@app.com", to, subject, body);
        await _smtp.SendMailAsync(message, ct);
    }
}
```
