using InvoiceAppAI.Model;
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
            var response = await _httpClient.GetAsync($"{_baseUrl}/by-description?description={Uri.EscapeDataString(name)}");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Invoice>(content, jsonSerializerOptions);
        }
    }
}
