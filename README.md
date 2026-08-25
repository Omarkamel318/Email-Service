# Email Service

A standalone .NET 8 Web API microservice responsible for sending emails via Gmail SMTP. Built to be consumed by multiple projects over HTTP, instead of each project implementing its own email logic.

## Features

- Send transactional emails (with optional attachments) via Gmail SMTP (MailKit)
- Asynchronous processing via an in-memory queue (`Channel<T>` + `BackgroundService`) — callers get an instant `202 Accepted` response
- Automatic retry with exponential backoff (Polly) for transient SMTP failures
- Centralized, reusable across multiple client projects

## Architecture

```
Client project 
        │  HTTP POST /api/email/send
        ▼
EmailController  →  IEmailQueue (Channel<T>)  →  202 Accepted returned immediately
                            │
                            ▼
                 EmailSenderWorker (BackgroundService)
                            │
                            ▼
                 SmtpEmailService (Polly retry pipeline)
                            │
                            ▼
                     Gmail SMTP server
```

## Tech Stack

- .NET 8 Web API
- MailKit / MimeKit — SMTP email sending
- Polly (`Microsoft.Extensions.Resilience`) — retry with exponential backoff
- `System.Threading.Channels` — in-process background queue

## Prerequisites

- .NET 8 SDK
- A Gmail account with:
  - 2-Step Verification enabled
  - An **App Password** generated (Google Account → Security → App Passwords)

## Setup

### 1. Clone and restore

```bash
git clone <repo-url>
cd EmailService
dotnet restore
```

### 2. Configure credentials (development)

Never commit real credentials to `appsettings.json`. Use User Secrets locally:

```bash
dotnet user-secrets init
dotnet user-secrets set "EmailSettings:SmtpServer" "smtp.gmail.com"
dotnet user-secrets set "EmailSettings:Port" "587"
dotnet user-secrets set "EmailSettings:SenderEmail" "yourmail@gmail.com"
dotnet user-secrets set "EmailSettings:SenderName" "Your App Name"
dotnet user-secrets set "EmailSettings:AppPassword" "your16charapppassword"
```

### 3. Run

```bash
dotnet run
```

Swagger UI will be available at `https://localhost:<port>/swagger` in development.

## API Reference

### `POST /api/email/send`

Queues an email for sending. Returns immediately; the email is sent asynchronously in the background.

**Content-Type:** `multipart/form-data` (supports file attachments)

| Field       | Type       | Required | Description                     |
|-------------|------------|----------|----------------------------------|
| `To`        | string     | yes      | Recipient email address          |
| `Subject`   | string     | yes      | Email subject                    |
| `Body`      | string     | yes      | HTML email body                  |
| `Attachments` | file[]   | no       | One or more file attachments     |

**Response**

```json
// 202 Accepted
{
  "message": "Email queued for sending"
}
```

Note: a `202` response means the request was accepted and queued — it does **not** guarantee the email was actually delivered by Gmail. Delivery failures are logged server-side.

## How to Call This Service From Another Project

Use a typed `HttpClient`, wrapped with retry and a circuit breaker, so a slow or unavailable Email Service never blocks or breaks the calling project's main flow (e.g. checkout):

```csharp
builder.Services.AddHttpClient<EmailApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["EmailService:BaseUrl"]!);
})
.AddResilienceHandler("email-circuit-breaker", pipelineBuilder =>
{
    pipelineBuilder.AddRetry(new HttpRetryStrategyOptions { MaxRetryAttempts = 2 });
    pipelineBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 4,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(20)
    });
});
```

Always wrap the call in a `try/catch` for `BrokenCircuitException` — a failed email should never fail the caller's main business operation.
