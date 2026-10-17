using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace Clinic_Management_System.Utilities
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("test@gmail.com", "rbhz lkpw dysp kyvy")
            };

            return client.SendMailAsync(
                new MailMessage(from: "test@gmail.com", to: email,subject,htmlMessage)
                {
                    IsBodyHtml = true
                
                });
        }
    }
    }
