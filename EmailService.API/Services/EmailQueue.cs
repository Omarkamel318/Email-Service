using EmailService.API.DTOs;
using EmailService.API.IServices;
using System.Threading.Channels;

namespace EmailService.API.Services
{
    public class EmailQueue : IEmailQueue
    {
        private readonly Channel<SendEmailRequest> _channel = Channel.CreateUnbounded<SendEmailRequest>();

        public void Enqueue(SendEmailRequest request)
        {
            _channel.Writer.TryWrite(request);
        }

        public IAsyncEnumerable<SendEmailRequest> DequeueAllAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAllAsync(cancellationToken);
        }
    }
}
