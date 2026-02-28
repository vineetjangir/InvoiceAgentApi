using InvoiceAgentApi.Model;
using System.Text.Json;

namespace InvoiceAgentApi.Services
{
    public class InvoiceApiService
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _baseUrl = "http://localhost:5000/api/invoices";
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        public async Task<List<Invoice>> GetInvoicesAsync()
        {
            var response = await _httpClient.GetAsync(_baseUrl);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Invoice>>(content, jsonSerializerOptions) ?? new List<Invoice>();
        }

        public async Task<Invoice?> GetInvoiceByNameAsync(string name)
        {
            var url = $"{_baseUrl}/by-description?description={Uri.EscapeDataString(name)}";
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Invoice>(content, jsonSerializerOptions);
        }

        public async Task<Invoice> CreateInvoice(CreateInvoiceRequest request)
        {
            var newInvoice = new Invoice
            {
                Id = 0, // ID will be set by the server
                Description = request.Description,
                Amount = request.Amount,
                Date = DateTime.UtcNow,
                Status = "Pending",
                Due = request.Due ?? DateTime.UtcNow.AddDays(30)
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(
                newInvoice,
                jsonSerializerOptions),
                System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_baseUrl, jsonContent);
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Invoice>(responseJson, jsonSerializerOptions)!;
        }

        public async Task MarkAsPaid(string invoiceId)
        {
            var request = new UpdateInvoiceRequest
            {
                Status = "Paid"
            };
            var jsonContent = new StringContent(JsonSerializer.Serialize(
                request,
                jsonSerializerOptions),
                System.Text.Encoding.UTF8, "application/json");
            var url = $"{_baseUrl}/{invoiceId}/status";
            var response = await _httpClient.PostAsync(url, jsonContent);
            response.EnsureSuccessStatusCode();
        }
    }
}
