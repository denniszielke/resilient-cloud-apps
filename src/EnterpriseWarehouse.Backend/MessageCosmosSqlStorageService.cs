using Microsoft.Azure.Cosmos;

public class MessageCosmosSqlStorageService : IMessageStorageService
{
    private readonly ILogger<MessageCosmosSqlStorageService> _logger;

    private CosmosClient _cosmosClient;

    private static string DatabaseName = "repair_parts";
    private static string ContainerName = "orders";

    public MessageCosmosSqlStorageService(ILogger<MessageCosmosSqlStorageService> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        AsyncHelper.RunAsync(async () =>
        {
            await CreateIfNotExistsAsync(DatabaseName, ContainerName);
        });
    }

    public async Task<bool> CreateIfNotExistsAsync(string databaseName, string containerName)
    {
        _logger.LogInformation("Creating Database");
        try
        {
            var db = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            if (db.StatusCode == System.Net.HttpStatusCode.Created)
            {
                ContainerProperties containerProperties = new ContainerProperties()
                {
                    Id = containerName,
                    PartitionKeyPath = "/repairPartId",
                    IndexingPolicy = new IndexingPolicy()
                    {
                        Automatic = false,
                        IndexingMode = IndexingMode.Lazy,
                    }
                };

                var container = await db.Database.CreateContainerIfNotExistsAsync(containerProperties);
            }
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, ex.Message);
        }

        return await Task.FromResult(true);
    }

    public async Task SaveOrderAsync(int repairPartId)
    {
        var newOrderId = Guid.NewGuid().ToString();
        _logger.LogTrace($"Saving order with repairPartId {repairPartId} with order id {newOrderId}");

        var newOrder = new RepairPartOrder
        {
            Id = newOrderId,
            RepairPartId = repairPartId,
            Timestamp = DateTime.UtcNow
        };

        var container = _cosmosClient.GetContainer(DatabaseName, ContainerName);
        if (container == null)
        {
            throw new Exception("Container was null");
        }

        var response = await container.CreateItemAsync(newOrder, new PartitionKey(repairPartId));
        _logger.LogTrace("Insert of item consumed {0} request units", response.RequestCharge);


        if (response.StatusCode == System.Net.HttpStatusCode.Created)
        {
            _logger.LogDebug($"Saved order with repairPartId {repairPartId} with order id {newOrderId}");
        }
        else
        {
            throw new Exception($"Save of order with repairPartId {repairPartId} with order id {newOrderId} resulted in {response.StatusCode}");
        }
    }
}