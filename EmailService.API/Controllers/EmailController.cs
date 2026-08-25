using EmailService.API.DTOs;
using EmailService.API.IServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace EmailService.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailQueue _emailQueue;

        public EmailController(IEmailQueue emailQueue)
        {
            _emailQueue = emailQueue;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromForm] SendEmailFormRequest form)
        {
            SendEmailRequest request = new SendEmailRequest
            {
                To = form.To,
                Subject = form.Subject,
                Body = form.Body,
                Attachments = new List<EmailAttachment>()
            };

            if(form.Attachments is not null )
            {
                foreach( IFormFile file in form.Attachments )
                {
                    byte[] fileBytes;
                    if(file.Length > 0)
                    {
                        MemoryStream ms = new MemoryStream();
                        file.CopyTo(ms);
                        fileBytes = ms.ToArray();

                        request.Attachments.Add(new EmailAttachment
                        {
                            FileName = file.FileName,
                            ContentType = file.ContentType,
                            Content = fileBytes
                        });
                    }
                }
            }

             _emailQueue.Enqueue(request);
             return Accepted(new { message = "Email queued for sending" });
        }

    }
}
