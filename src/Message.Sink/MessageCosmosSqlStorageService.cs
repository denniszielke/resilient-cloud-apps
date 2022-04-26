using Microsoft.Azure.Cosmos;

public class MessageCosmosSqlStorageService : IMessageStorageService
{
    private readonly ILogger<MessageCosmosSqlStorageService> _logger;

    private CosmosClient _cosmosClient;

    private static string DatabaseName = "messages";
    private static string ContainerName = "data";

    public MessageCosmosSqlStorageService(ILogger<MessageCosmosSqlStorageService> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
         AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageCosmosSqlStorageService.DatabaseName, MessageCosmosSqlStorageService.ContainerName);
            });
    }

    public async void Initialize(string tableName)
    {
       AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageCosmosSqlStorageService.DatabaseName, MessageCosmosSqlStorageService.ContainerName);
            });
    }

    public async Task<bool> CreateIfNotExistsAsync(string databaseName, string containerName)
    {
        _logger.LogInformation("Creating Database");
        try
        {
            var db = await _cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            if ( db.StatusCode == System.Net.HttpStatusCode.Created )
            {
                ContainerProperties containerProperties = new ContainerProperties()
                {
                    Id = containerName,
                    PartitionKeyPath = "/id",
                    IndexingPolicy = new IndexingPolicy()
                    {
                        Automatic = false,
                        IndexingMode = IndexingMode.Lazy,
                    }
                };

                var container = await db.Database.CreateContainerIfNotExistsAsync(containerProperties);
            }
        }
        catch (System.Exception ex)
        {
            _logger.LogError(ex, ex.Message);
        }                

        return await Task.FromResult<bool>(true);
    }

     public async Task<DeviceMessage> GetById(Container container, string messageId)
    {
        DeviceMessage result = null;

        try
        {
            var query = new QueryDefinition($"select * from c where c.id = '{messageId}'");

            var iterator = container.GetItemQueryIterator<DeviceMessage>(
                query,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(messageId)
                });

            if (iterator.HasMoreResults)
            {
                var item = await iterator.ReadNextAsync();
                result = item.FirstOrDefault();
            }
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception ex)
        {
            return null;
        }
        return result;
    }

    public async Task<MessageStatus> SaveMessage(DeviceMessage message)
    {
        _logger.LogTrace($"Saving message in partition {message.Name} with rowkey {message.Id}");

        MessageStatus status = MessageStatus.Failed;
        ItemResponse<DeviceMessage> response = null;

        if (message == null || string.IsNullOrWhiteSpace(message.Name) || string.IsNullOrWhiteSpace(message.Id)) 
        {   
            _logger.LogCritical("Message data was null");
            return status;
        }

        try
        {
            var container = _cosmosClient.GetContainer(MessageCosmosSqlStorageService.DatabaseName, MessageCosmosSqlStorageService.ContainerName);

            if (container == null){
                _logger.LogCritical("Container was null");
                return MessageStatus.Failed;
            }

            var existingItem = await GetById(container, message.Id);

            if (existingItem != null){
                response = await container.UpsertItemAsync<DeviceMessage>(message, new PartitionKey(message.Id), new PatchItemRequestOptions());
            }
            else{
                 response = await container.CreateItemAsync<DeviceMessage>(message, new PartitionKey(message.Id));
            }          

            _logger.LogTrace("Insert of item consumed {0} request units", response.RequestCharge);

            if (response == null){
                _logger.LogCritical("Response was null");
                return MessageStatus.Failed;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Created){
                status = MessageStatus.Ok;
                _logger.LogDebug($"Saved message in partition {message.Name} with rowkey {message.Id}");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.OK){
                status = MessageStatus.Ok;
                _logger.LogDebug($"Updated message in partition {message.Name} with rowkey {message.Id}");
            }
            else{
                status = MessageStatus.Failed;
                _logger.LogInformation($"Save of message in partition {message.Name} with rowkey {message.Id} resulted in {response.StatusCode}");
            }
        
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            status = MessageStatus.Throttled;
            _logger.LogError(ex, ex.Message);
        }
        catch (CosmosException ex)
        {
            status = MessageStatus.Failed;
            _logger.LogError(ex, ex.Message);
        }
        catch (System.Exception ex)
        {
            status = MessageStatus.Failed;
            _logger.LogError(ex, ex.Message);
        }

        
        return status;
    }
}