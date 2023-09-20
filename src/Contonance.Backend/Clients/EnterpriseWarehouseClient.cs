using Contonance.Shared;

namespace Contonance.Backend.Clients
{
    public class EnterpriseWarehouseClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<EnterpriseWarehouseClient> _logger;

        public EnterpriseWarehouseClient(IHttpClientFactory httpClientFactory, ILogger<EnterpriseWarehouseClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task OrderRepairPartAsync(int repairPartId)
        {
            var client = _httpClientFactory.CreateClient("Sink");
            var response = await client.PostAsJsonAsync("/api/repairparts/order", repairPartId);
            response.EnsureSuccessStatusCode();
        }
    }
}