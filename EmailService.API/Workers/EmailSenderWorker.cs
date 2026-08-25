
using EmailService.API.IServices;

namespace EmailService.API.Workers
{
    public class EmailSenderWorker : BackgroundService
    {
        private readonly IEmailQueue _queue;
        private readonly ILogger<EmailSenderWorker> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public EmailSenderWorker(IEmailQueue queue, ILogger<EmailSenderWorker> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _queue = queue;
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach(var request in _queue.DequeueAllAsync(stoppingToken))
            {
                IServiceScope scope = _serviceScopeFactory.CreateScope();
                IEmailService emailService = scope.ServiceProvider.GetService<IEmailService>();

                try
                {
                    await emailService.SendEmailAsync(request);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {ToEmail}", request.To);
                }
            }
        }
    }
}
