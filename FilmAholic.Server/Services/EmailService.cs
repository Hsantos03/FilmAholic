using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace FilmAholic.Server.Services;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string email, string verificationToken, string userId);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendVerificationEmailAsync(string email, string verificationToken, string userId)
    {
        try
        {
            var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
            var smtpUser = _configuration["EmailSettings:SmtpUser"];
            var smtpPassword = _configuration["EmailSettings:SmtpPassword"];
            var fromEmail = _configuration["EmailSettings:FromEmail"] ?? smtpUser;
            var fromName = _configuration["EmailSettings:FromName"] ?? "FilmAholic";
            var baseUrl = _configuration["EmailSettings:BaseUrl"] ?? "https://localhost:7277";

            // Em desenvolvimento, se não houver configuração SMTP, apenas logamos
            if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
            {
                _logger.LogWarning($"Email não configurado. Token de verificação para {email}: {verificationToken}");
                _logger.LogWarning($"URL de verificação: {baseUrl}/api/autenticacao/confirmar-email?userId={userId}&token={verificationToken}");
                return;
            }

            var verificationUrl = $"{baseUrl}/api/autenticacao/confirmar-email?userId={userId}&token={Uri.EscapeDataString(verificationToken)}";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Confirme o seu email - FilmAholic";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
                            <h2 style='color: #333;'>Bem-vindo ao FilmAholic!</h2>
                            <p>Obrigado por se registar. Para ativar a sua conta, clique no botão abaixo ou copie e cole o link no seu navegador:</p>
                            <p style='text-align: center; margin: 30px 0;'>
                                <a href='{verificationUrl}' style='background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; display: inline-block;'>Confirmar Email</a>
                            </p>
                            <p>Ou copie este link:</p>
                            <p style='word-break: break-all; color: #666;'>{verificationUrl}</p>
                            <p style='margin-top: 30px; color: #999; font-size: 12px;'>Se não criou uma conta no FilmAholic, pode ignorar este email.</p>
                        </div>
                    </body>
                    </html>",
                TextBody = $"Bem-vindo ao FilmAholic!\n\nPara confirmar o seu email, aceda a: {verificationUrl}\n\nSe não criou uma conta, pode ignorar este email."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtpUser, smtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation($"Email de verificação enviado para {email}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Erro ao enviar email de verificação para {email}");
            // Em desenvolvimento, continuamos mesmo se o email falhar
            throw;
        }
    }
}

