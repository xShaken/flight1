using flight.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace flight.Services
{
    public class PayMongoService
    {
        private readonly HttpClient _httpClient;
        private readonly string _secretKey;

        // Constructor to initialize HttpClient and secret key
        public PayMongoService(HttpClient httpClient, PayMongoServiceConfiguration config)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _secretKey = config?.SecretKey ?? throw new ArgumentNullException(nameof(config));
        }

        // Method to create a payment intent
        public async Task<string> CreatePaymentIntent(decimal amount)
        {
            try
            {
                // Log the incoming amount
                Console.WriteLine($"Creating payment intent for amount: {amount}");

                // Ensure the amount is valid
                if (amount <= 0)
                {
                    throw new ArgumentException("Amount must be greater than zero.", nameof(amount));
                }

                // Create the payload for the PayMongo API
                var payload = new
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

                // Serialize the payload to JSON
                var jsonPayload = JsonSerializer.Serialize(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                // Set the authorization header
                var authHeader = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_secretKey}:"));
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);

                // Log the request payload
                Console.WriteLine($"Sending request to PayMongo API: {jsonPayload}");

                // Send the request to the PayMongo API
                var response = await _httpClient.PostAsync("payment_intents", content);

                // Log the response status
                Console.WriteLine($"PayMongo API response status: {response.StatusCode}");

                // Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    // Read and parse the response
                    var responseData = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"PayMongo API response data: {responseData}");

                    // Deserialize the response
                    var paymentIntent = JsonSerializer.Deserialize<PaymentIntentResponse>(responseData);

                    // Ensure the payment intent ID is not null or empty
                    if (string.IsNullOrEmpty(paymentIntent?.Data?.Id))
                    {
                        throw new Exception("Failed to extract payment intent ID from the response.");
                    }

                    // Return the payment intent ID
                    return paymentIntent.Data.Id;
                }
                else
                {
                    // Log the error response
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"PayMongo API error response: {errorResponse}");
                    throw new HttpRequestException($"Failed to create payment intent. Status: {response.StatusCode}, Response: {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in CreatePaymentIntent: {ex.Message}");
                throw; // Re-throw the exception to be handled by the caller
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