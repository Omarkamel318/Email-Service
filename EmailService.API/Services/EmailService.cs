using EmailService.API.DTOs;
using EmailService.API.IServices;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Polly;
using Polly.Registry;

namespace EmailService.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly ResiliencePipeline _retryPipeline;
        private EmailSettings _settings;
        public EmailService(IOptions<EmailSettings> options, ResiliencePipelineProvider<string> pipelineProvider)
        {
            _settings = options.Value;
            _retryPipeline = pipelineProvider.GetPipeline("email-retry-pipeline");
        }
        public async Task SendEmailAsync(SendEmailRequest request)
        {
            await _retryPipeline.ExecuteAsync(async cancellationToken =>
            {
                MimeMessage email = new MimeMessage();

                email.Sender = MailboxAddress.Parse(_settings.SenderEmail);

                email.Subject = request.Subject;

                email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

                email.To.Add(MailboxAddress.Parse(request.To));

                BodyBuilder builder = new BodyBuilder();

                if (request.Attachments is not null)
                {
                    foreach (var attachment in request.Attachments)
                    {
                        builder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
                    }
                }

                builder.HtmlBody = request.Body;

                email.Body = builder.ToMessageBody();

                using SmtpClient smtp = new SmtpClient();
                await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);
            });
            

        }
    }
}
