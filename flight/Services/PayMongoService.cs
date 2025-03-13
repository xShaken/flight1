using flight.Models;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace flight.Services
{
    namespace flight.Services
    {
        public class PayMongoService
        {
            private readonly HttpClient _httpClient;
            private readonly PayMongoServiceConfiguration _config;

            public PayMongoService(HttpClient httpClient, PayMongoServiceConfiguration config)
            {
                _httpClient = httpClient;
                _config = config;

                // Set the authorization header using the secret key from the configuration
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(_config.SecretKey)));
            }

            // Method to create a payment intent
            public async Task<string> CreatePaymentIntent(decimal amount)
            {
                try
                {
                    var requestBody = new
                    {
                        data = new
                        {
                            attributes = new
                            {
                                amount = (int)(amount * 100), // Convert to cents
                                payment_method_allowed = new[] { "gcash" },
                                currency = "PHP"
                            }
                        }
                    };

                    var json = JsonConvert.SerializeObject(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // Correct the URL to avoid duplicate /v1
                    var response = await _httpClient.PostAsync("v1/payment_intents", content);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var responseObject = JsonConvert.DeserializeObject<PaymentIntentResponse>(responseContent);

                    // Extract the payment intent ID
                    if (responseObject?.Data?.Id == null)
                    {
                        throw new Exception("Failed to extract payment intent ID from the response.");
                    }

                    return responseObject.Data.Id;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in CreatePaymentIntent: {ex.Message}");
                    throw;
                }
            }




            // Class to represent the PayMongo API response
            private class PaymentIntentResponse
            {
                public PaymentIntentData Data { get; set; }
            }

            // Class to represent the payment intent data
            private class PaymentIntentData
            {
                public string Id { get; set; }
                public PaymentIntentAttributes Attributes { get; set; }
            }

            // Class to represent the payment intent attributes
            private class PaymentIntentAttributes
            {
                public int Amount { get; set; }
                public string Currency { get; set; }
                public string Status { get; set; }
                public string[] PaymentMethodAllowed { get; set; }
                public string Description { get; set; }
                public bool Livemode { get; set; }
                public int OriginalAmount { get; set; }
                public string StatementDescriptor { get; set; }
                public object LastPaymentError { get; set; }
                public object[] Payments { get; set; }
                public object NextAction { get; set; }
                public object PaymentMethodOptions { get; set; }
                public object Metadata { get; set; }
                public object SetupFutureUsage { get; set; }
                public long CreatedAt { get; set; }
                public long UpdatedAt { get; set; }
            }
        }
    }
}