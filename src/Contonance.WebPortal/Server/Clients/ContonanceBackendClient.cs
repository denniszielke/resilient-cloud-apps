using System.Net;
using System.Text.Json;
using Contonance.Shared;

namespace Contonance.WebPortal.Server.Clients;

public class ContonanceBackendClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ContonanceBackendClient> _logger;

    public ContonanceBackendClient(IHttpClientFactory httpClientFactory, ILogger<ContonanceBackendClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<IList<RepairReport>> GetAllRepairReports()
    {
        var client = _httpClientFactory.CreateClient("Sink");
        var response = await client.GetAsync("/api/message/receive");

        Console.WriteLine(response.StatusCode);

        string responseBody = await response.Content.ReadAsStringAsync();
        Console.WriteLine(responseBody);

        try
        {
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<IList<RepairReport>>(responseBody)!;
            }
            else
            {
                Console.WriteLine(response.StatusCode);
                return null;
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            // TODO
            throw;
        }
    }
}