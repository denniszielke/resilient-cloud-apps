using Microsoft.AspNetCore.Mvc;
using Contonance.Shared;
using Azure.Messaging.EventHubs.Producer;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using System.Text;
using Contonance.WebPortal.Server.Clients;

namespace Contonance.WebPortal.Server.Controllers;

[ApiController]
[Route("[controller]")]
public class RepairReportsController : ControllerBase
{

    private readonly ContonanceBackendClient _contonanceBackendClient;
    private readonly EventHubProducerClient _eventHubClient;
    private readonly ILogger<RepairReportsController> _logger;

    public RepairReportsController(ContonanceBackendClient contonanceBackendClient, EventHubProducerClient eventHubClient, ILogger<RepairReportsController> logger)
    {
        _contonanceBackendClient = contonanceBackendClient;
        _eventHubClient = eventHubClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IList<RepairReport>> Get()
    {
        return await _contonanceBackendClient.GetAllRepairReports();
    }

    [HttpPost]
    public async Task Add([FromBody] RepairReport repairReport)
    {
        repairReport.Id = Guid.NewGuid();
        _logger.LogDebug($"received repairReport {repairReport.Id}:{repairReport.Title}");

        using EventDataBatch eventBatch = await _eventHubClient.CreateBatchAsync();
        string jsonString = JsonSerializer.Serialize(repairReport, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var added = eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes(jsonString)));
        if (!added)
        {
            throw new Exception("Could not add repairReport to batch");
        }

        await _eventHubClient.SendAsync(eventBatch);
        _logger.LogDebug($"send repairReport {repairReport.Title} to eventhub");
    }
}
