using EmailService.API.DTOs;

namespace EmailService.API.IServices
{
    public interface IEmailService
    {
        Task SendEmailAsync(SendEmailRequest request);

    }
}
