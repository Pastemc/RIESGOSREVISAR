using System.Net;
using System.Net.Mail;

namespace RiesgosElor.Services;

public class EmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task<bool> EnviarCorreoAprobacionAsync(string correoDestino, string nombreUsuario)
    {
        if (string.IsNullOrWhiteSpace(correoDestino)) return false;

        var host = _config["Smtp:Host"] ?? "smtp.gmail.com";
        var portStr = _config["Smtp:Port"] ?? "587";
        int.TryParse(portStr, out int port);
        if (port == 0) port = 587;

        var user = _config["Smtp:User"] ?? "";
        var pass = _config["Smtp:Password"] ?? "";
        var from = _config["Smtp:From"] ?? "gir.notificaciones@elor.com.pe";
        bool enableSsl = true;
        var appUrl = _config["AppUrl"] ?? "http://10.117.8.204:5000";

        string asunto = "Acceso Aprobado - Sistema GIR Electro Oriente S.A.";
        string htmlBody = $@"
<div style=""font-family:'Segoe UI',Helvetica,Arial,sans-serif; max-width:600px; margin:0 auto; border:1px solid #e2e8f0; border-radius:12px; overflow:hidden; box-shadow:0 4px 12px rgba(0,0,0,0.05);"">
    <div style=""background:linear-gradient(135deg, #003B70 0%, #0062B8 100%); padding:24px; text-align:center; border-bottom:4px solid #FFDD00;"">
        <h2 style=""color:#ffffff; margin:0; font-size:22px; font-weight:800;"">Electro Oriente S.A.</h2>
        <span style=""color:#FFDD00; font-size:12px; font-weight:700; text-transform:uppercase; letter-spacing:1px;"">Sistema de Gestión Integral de Riesgos (GIR)</span>
    </div>
    <div style=""padding:32px; background-color:#ffffff; color:#334155; line-height:1.6;"">
        <h3 style=""color:#003B70; margin-top:0; font-size:18px;"">¡Tu Solicitud de Acceso ha sido Aprobada!</h3>
        <p style=""margin-bottom:16px;"">Estimado(a) <strong>{WebUtility.HtmlEncode(nombreUsuario)}</strong>,</p>
        <p>Nos complace informarle que su solicitud de acceso al <strong>Sistema GIR de Electro Oriente S.A.</strong> ha sido autorizada satisfactoriamente por el Administrador.</p>

        <div style=""background-color:#EBF3FA; padding:16px; border-left:4px solid #0062B8; margin:24px 0; border-radius:6px;"">
            <strong style=""color:#003B70;"">Estado de Cuenta:</strong> <span style=""color:#059669; font-weight:bold;"">✔ ACTIVO</span><br/>
            <strong style=""color:#003B70;"">Correo Registrado:</strong> {WebUtility.HtmlEncode(correoDestino)}
        </div>

        <p>Ya puede iniciar sesión con sus credenciales correspondientes:</p>

        <div style=""text-align:center; margin:32px 0;"">
            <a href=""{appUrl}/login"" style=""background:linear-gradient(135deg, #0062B8 0%, #004F94 100%); color:#ffffff; padding:14px 32px; border-radius:8px; text-decoration:none; font-weight:bold; font-size:14px; display:inline-block; box-shadow:0 4px 10px rgba(0,98,184,0.3);"">Ingresar al Sistema GIR</a>
        </div>
    </div>
    <div style=""background-color:#f8fafc; padding:16px; text-align:center; font-size:11px; color:#64748b; border-top:1px solid #e2e8f0;"">
        Mensaje automático generado por el Sistema GIR &mdash; Electro Oriente S.A.<br/>
        Por favor no responda a este correo electrónico.
    </div>
</div>";

        try
        {
            if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
            {
                _logger.LogWarning("[EmailService] Servidor SMTP sin credenciales configuradas en appsettings.json. Notificación simulada para {Correo}", correoDestino);
                return true;
            }

            using var message = new MailMessage();
            message.From = new MailAddress(from, "Sistema GIR - Electro Oriente");
            message.To.Add(correoDestino);
            message.Subject = asunto;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient(host, port);
            client.Credentials = new NetworkCredential(user, pass);
            client.EnableSsl = enableSsl;

            await client.SendMailAsync(message);
            _logger.LogInformation("[EmailService] Correo de aprobación enviado exitosamente a {Correo}", correoDestino);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailService] Error al enviar correo de notificación a {Correo}", correoDestino);
            return false;
        }
    }
}
