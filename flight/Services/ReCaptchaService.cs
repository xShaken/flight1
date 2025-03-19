using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace flight.Services
{
    public class ReCaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ReCaptchaService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task<bool> VerifyReCaptcha(string token)
        {
            var secretKey = _configuration["GoogleReCaptcha:SecretKey"];
            var response = await _httpClient.GetStringAsync(
                $"https://www.google.com/recaptcha/api/siteverify?secret={secretKey}&response={token}"
            );

            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.GetProperty("success").GetBoolean();
        }
    }
}