using System.Text;
using System.Text.Json;

namespace limbo.Services;

public class EmailService
{
    private readonly EmailSettings _settings;
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;

    public EmailService(EmailSettings settings, IConfiguration config, HttpClient httpClient)
    {
        _settings = settings;
        _config = config;
        _httpClient = httpClient;
    }

    public async Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var appUrl = _config["AppUrl"];
        var link = $"{appUrl}/auth/verifyemail?token={token}";

        var payload = new
        {
            sender = new { email = _settings.From, name = "User Manager" },
            to = new[] { new { email = toEmail } },
            subject = "Verify your email address",
            textContent = $"Please verify your email by clicking the link below:\n\n{link}"
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", _settings.Password);
        request.Headers.Add("accept", "application/json");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Brevo API error: {response.StatusCode} - {error}");
        }
    }
}