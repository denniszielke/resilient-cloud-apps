using Microsoft.Azure.Cosmos;

public class MessageCosmosTableStorageService : IMessageStorageService
{
    private readonly ILogger<MessageCosmosTableStorageService> _logger;

    private CosmosClient _cosmosClient;

    private static string TableName = "messages";
    private static string DatabaseName = "TablesDB";

    public MessageCosmosTableStorageService(ILogger<MessageCosmosTableStorageService> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
         AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageCosmosTableStorageService.DatabaseName, MessageCosmosTableStorageService.TableName);
            });
    }

    public async void Initialize(string tableName)
    {
       AsyncHelper.RunAsync(async () => {
               await CreateIfNotExistsAsync(MessageCosmosTableStorageService.DatabaseName, MessageCosmosTableStorageService.TableName);
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

    public async Task<MessageStatus> SaveMessage(DeviceMessage message)
    {
        _logger.LogTrace($"Saving message in partition {message.Name} with rowkey {message.Id}");

        MessageStatus status = MessageStatus.Failed;
        ItemResponse<DeviceMessage> response = null;

        try
        {
            var container = _cosmosClient.GetContainer(MessageCosmosTableStorageService.DatabaseName, MessageCosmosTableStorageService.TableName);

            if (container == null){
                _logger.LogCritical("Container was null");
                return MessageStatus.Failed;
            }

            response = await container.CreateItemAsync<DeviceMessage>(message, new PartitionKey(message.Id));

            _logger.LogTrace("Insert of item consumed {0} request units", response.RequestCharge);

            if (response == null){
                _logger.LogCritical("Response was null");
                return MessageStatus.Failed;
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Created){
                status = MessageStatus.Ok;
                _logger.LogDebug($"Saved message in partition {message.Name} with rowkey {message.Id}");
            }
            else{
                status = MessageStatus.Failed;
                _logger.LogInformation($"Save of message in partition {message.Name} with rowkey {message.Id} resulted in {response.StatusCode}");
            }
        
        }
        catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            status = MessageStatus.Failed;
            _logger.LogInformation("Insert of item consumed {0} request units", response.RequestCharge);
        }
        catch (System.Exception ex)
        {
            status = MessageStatus.Failed;
            _logger.LogError(ex, ex.Message);
        }

        
        return status;
    }
}