using EmailService.API.DTOs;

namespace EmailService.API.IServices
{
    public interface IEmailQueue
    {
        void Enqueue(SendEmailRequest request);
        IAsyncEnumerable<SendEmailRequest> DequeueAllAsync(CancellationToken cancellationToken);
    }
}
