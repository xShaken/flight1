using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class PaymongoService
{
    private readonly HttpClient _httpClient;
    private readonly string _secretKey;

    public PaymongoService(HttpClient httpClient, string secretKey)
    {
        _httpClient = httpClient;
        _secretKey = secretKey;
    }

    public async Task<string> CreatePaymentLinkAsync(decimal amount, string description, string remarks)
    {
        try
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri("https://api.paymongo.com/v1/links"),
                Headers =
            {
                { "accept", "application/json" },
                { "authorization", $"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(_secretKey + ":"))}" },
            },
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = (int)(amount * 100), // Convert to cents
                            description,
                            remarks
                        }
                    }
                }), Encoding.UTF8, "application/json")
            };

            using (var response = await _httpClient.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Paymongo API Response: {body}"); // Log the response

                var result = JsonConvert.DeserializeObject<PaymongoResponse>(body);
                return result.Data.Attributes.CheckoutUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CreatePaymentLinkAsync: {ex.Message}");
            return null;
        }
    }

    private class PaymongoResponse
    {
        public PaymongoData Data { get; set; }
    }

    private class PaymongoData
    {
        public PaymongoAttributes Attributes { get; set; }
    }

    private class PaymongoAttributes
    {
        public string CheckoutUrl { get; set; }
    }
}