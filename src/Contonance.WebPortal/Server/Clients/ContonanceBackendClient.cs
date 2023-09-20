using System.Text.Json;
using Contonance.Shared;

namespace Contonance.WebPortal.Server.Clients;

public class ContonanceBackendClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContonanceBackendClient> _logger;

    public ContonanceBackendClient(HttpClient httpClient, IConfiguration configuration, ILogger<ContonanceBackendClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(configuration.GetNoEmptyStringOrThrow("CONTONANCE_BACKEND_URL"));
    }

    public async Task<IList<RepairReport>> GetAllRepairReports()
    {
        var response = await _httpClient.GetAsync("/api/repairreports");
        _logger.LogDebug(response.StatusCode.ToString());

        string responseBody = await response.Content.ReadAsStringAsync();
        _logger.LogDebug(responseBody);

        response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<IList<RepairReport>>(responseBody, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}