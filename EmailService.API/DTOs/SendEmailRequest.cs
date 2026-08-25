namespace EmailService.API.DTOs
{
    public class SendEmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public IList<EmailAttachment>? Attachments { get; set; }
    }
}
