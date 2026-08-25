using EmailService.API.IServices;
using EmailService.API.Services;
using EmailService.API.Workers;
using Polly;
using Polly.Retry;
namespace EmailService.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
             
            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            builder.Services.AddScoped<IEmailService, Services.EmailService>();

            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddResiliencePipeline("email-retry-pipeline", pipelineBuilder =>
            {
                pipelineBuilder.AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(2),
                    ShouldHandle = new PredicateBuilder().Handle<Exception>()
                });
            });

            builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
            builder.Services.AddHostedService<EmailSenderWorker>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
